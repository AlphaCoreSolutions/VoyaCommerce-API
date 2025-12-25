using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
	private readonly string[] _requiredPermissions;

	// Supports:
	// [RequirePermission(Permissions.OrdersView)]
	// [RequirePermission(Permissions.OrdersView, Permissions.OrdersManage)]
	public RequirePermissionAttribute(params string[] permissions)
	{
		_requiredPermissions = permissions
			.Where(p => !string.IsNullOrWhiteSpace(p))
			.Select(p => p.Trim().ToLowerInvariant())
			.Distinct()
			.ToArray();
	}

	public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
	{
		var user = context.HttpContext.User;

		if (user?.Identity?.IsAuthenticated != true)
		{
			context.Result = new UnauthorizedResult();
			return;
		}

		// Prefer NameIdentifier; fallback to "sub" or "id"
		var userIdString =
			user.FindFirst(ClaimTypes.NameIdentifier)?.Value
			?? user.FindFirst("sub")?.Value
			?? user.FindFirst("id")?.Value;

		if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out var userId))
		{
			context.Result = new UnauthorizedResult();
			return;
		}

		// If no permissions were specified, treat it as configuration error -> forbid
		if (_requiredPermissions.Length == 0)
		{
			context.Result = new ForbidResult();
			return;
		}

		var dbContext = context.HttpContext.RequestServices.GetRequiredService<VoyaDbContext>();

		// Request-level cache so multiple actions in same request don't re-query
		const string cacheKey = "__nexus_permissions_cache__";
		if (context.HttpContext.Items.TryGetValue(cacheKey, out var cachedObj) &&
			cachedObj is PermissionCache cached &&
			cached.UserId == userId)
		{
			if (!HasPermission(cached, _requiredPermissions))
			{
				context.Result = new ForbidResult();
			}
			return;
		}

		var userEntity = await dbContext.Users
			.AsNoTracking()
			.Include(u => u.NexusRole)
			.FirstOrDefaultAsync(u => u.Id == userId);

		if (userEntity?.NexusRole == null)
		{
			context.Result = new ForbidResult();
			return;
		}

		// Super Admin bypass
		if (userEntity.NexusRole.IsSuperAdmin)
		{
			return;
		}

		// Normalize role permissions into a HashSet for safe checking
		var rolePermissionSet = userEntity.NexusRole.Permissions
			.Where(p => !string.IsNullOrWhiteSpace(p))
			.Select(p => p.Trim().ToLowerInvariant())
			.ToHashSet();


		var permissionCache = new PermissionCache(userId, rolePermissionSet);
		context.HttpContext.Items[cacheKey] = permissionCache;

		if (!HasPermission(permissionCache, _requiredPermissions))
		{
			context.Result = new ForbidResult();
			return;
		}

	}

	private static bool HasPermission(PermissionCache cache, string[] required)
	{
		// OR semantics: user must have at least one of the required permissions
		// If you want AND semantics later, we can add a parameter or attribute.
		foreach (var p in required)
		{
			if (cache.Permissions.Contains(p))
				return true;
		}
		return false;
	}

	

	private sealed record PermissionCache(Guid UserId, HashSet<string> Permissions);
}
