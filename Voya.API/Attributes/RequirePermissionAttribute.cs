using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
	private readonly string _permission;

	public RequirePermissionAttribute(string permission)
	{
		_permission = permission;
	}

	public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
	{
		// 1. Get User ID
		var user = context.HttpContext.User;
		if (!user.Identity?.IsAuthenticated ?? true)
		{
			context.Result = new UnauthorizedResult();
			return;
		}

		var userIdString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (userIdString == null)
		{
			context.Result = new UnauthorizedResult();
			return;
		}

		var userId = Guid.Parse(userIdString);

		// 2. Get DB Context
		var dbContext = context.HttpContext.RequestServices.GetRequiredService<VoyaDbContext>();

		// 3. Fetch User and their Role permissions
		// Note: For high performance, cache this query using IMemoryCache later!
		var userEntity = await dbContext.Users
			.Include(u => u.NexusRole) // Ensure Role is loaded
			.FirstOrDefaultAsync(u => u.Id == userId);

		if (userEntity?.NexusRole == null)
		{
			context.Result = new ForbidResult(); // Not staff
			return;
		}

		// 4. Super Admin Bypass
		if (userEntity.NexusRole.IsSuperAdmin)
		{
			return; // Allowed to do anything
		}

		// 5. Check Specific Permission
		if (!userEntity.NexusRole.Permissions.Contains(_permission))
		{
			context.Result = new ForbidResult(); // 403 Forbidden
			return;
		}
	}
}