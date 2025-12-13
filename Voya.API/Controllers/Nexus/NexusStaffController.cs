using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Voya.API.Attributes;
using Voya.Core.Constants;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/staff")]
public class NexusStaffController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public NexusStaffController(VoyaDbContext context)
	{
		_context = context;
	}

	private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

	// --- 0. PERMISSION DISCOVERY ---

	// GET: api/v1/nexus/staff/permissions/list
	[HttpGet("permissions/list")]
	public IActionResult GetAllPermissions()
	{
		// Reflection to get all string constants from Permissions class
		var perms = typeof(Permissions)
			.GetFields()
			.Where(f => f.IsLiteral && !f.IsInitOnly)
			.Select(f => new { Group = f.Name, Value = f.GetValue(null) })
			.ToList();

		return Ok(perms);
	}

	// --- 1. ROLE MANAGEMENT ---

	// GET: List all roles
	[HttpGet("roles")]
	[RequirePermission(Permissions.StaffManage)]
	public async Task<IActionResult> GetRoles()
	{
		// Auto-Seed Default Roles if empty (Helper for first run)
		if (!await _context.NexusRoles.AnyAsync())
		{
			_context.NexusRoles.AddRange(
				new NexusRole { Name = "Super Admin", Description = "God Mode", IsSuperAdmin = true, Permissions = new() },
				new NexusRole { Name = "Support Agent", Description = "Handle Tickets", Permissions = new() { "users.view", "orders.view" } }
			);
			await _context.SaveChangesAsync();
		}

		// Return list with user counts
		var roles = await _context.NexusRoles
			.Include(r => r.Users)
			.Select(r => new
			{
				r.Id,
				r.Name,
				r.Description,
				r.IsSuperAdmin,
				PermissionsCount = r.Permissions.Count,
				UserCount = r.Users.Count
			})
			.ToListAsync();

		return Ok(roles);
	}

	// GET: Single Role Details
	[HttpGet("roles/{id}")]
	[RequirePermission(Permissions.StaffManage)]
	public async Task<IActionResult> GetRoleDetails(Guid id)
	{
		var role = await _context.NexusRoles.FindAsync(id);
		if (role == null) return NotFound();
		return Ok(role);
	}

	// POST: Create New Role
	[HttpPost("roles")]
	[RequirePermission(Permissions.StaffManage)]
	public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto request)
	{
		if (await _context.NexusRoles.AnyAsync(r => r.Name == request.Name))
			return BadRequest("Role name already exists.");

		var role = new NexusRole
		{
			Name = request.Name,
			Description = request.Description,
			IsSuperAdmin = false,
			Permissions = request.Permissions ?? new List<string>()
		};

		_context.NexusRoles.Add(role);

		// Log Action
		_context.SystemAuditLogs.Add(new SystemAuditLog
		{
			AdminUserId = GetUserId(),
			Action = "Created Role",
			EntityId = role.Id.ToString(),
			NewValue = role.Name
		});

		await _context.SaveChangesAsync();
		return Ok(role);
	}

	// PUT: Update Role Details (Name/Desc)
	[HttpPut("roles/{id}")]
	[RequirePermission(Permissions.StaffManage)]
	public async Task<IActionResult> UpdateRoleDetails(Guid id, [FromBody] CreateRoleDto request)
	{
		var role = await _context.NexusRoles.FindAsync(id);
		if (role == null) return NotFound();

		if (role.IsSuperAdmin)
			return BadRequest("Cannot modify Super Admin details.");

		role.Name = request.Name;
		role.Description = request.Description;

		await _context.SaveChangesAsync();
		return Ok(role);
	}

	// POST: Update Permissions Only (Toggle Matrix)
	[HttpPost("roles/{id}/permissions")]
	[RequirePermission(Permissions.StaffManage)]
	public async Task<IActionResult> UpdatePermissions(Guid id, [FromBody] List<string> newPermissions)
	{
		var role = await _context.NexusRoles.FindAsync(id);
		if (role == null) return NotFound();

		if (role.IsSuperAdmin)
			return BadRequest("Cannot modify permissions of Super Admin.");

		role.Permissions = newPermissions;

		// Log Action
		_context.SystemAuditLogs.Add(new SystemAuditLog
		{
			AdminUserId = GetUserId(),
			Action = "Updated Permissions",
			EntityId = role.Id.ToString(),
			NewValue = $"Count: {newPermissions.Count}"
		});

		await _context.SaveChangesAsync();
		return Ok(role);
	}

	// DELETE: Remove Role
	[HttpDelete("roles/{id}")]
	[RequirePermission(Permissions.StaffManage)]
	public async Task<IActionResult> DeleteRole(Guid id)
	{
		var role = await _context.NexusRoles.Include(r => r.Users).FirstOrDefaultAsync(r => r.Id == id);
		if (role == null) return NotFound();

		if (role.IsSuperAdmin) return BadRequest("Cannot delete Super Admin.");
		if (role.Users.Any()) return BadRequest($"Cannot delete role. It is assigned to {role.Users.Count} active users.");

		_context.NexusRoles.Remove(role);

		// Log Action
		_context.SystemAuditLogs.Add(new SystemAuditLog
		{
			AdminUserId = GetUserId(),
			Action = "Deleted Role",
			EntityId = role.Id.ToString(),
			OldValue = role.Name
		});

		await _context.SaveChangesAsync();
		return Ok("Role deleted.");
	}

	// --- 2. EMPLOYEE MANAGEMENT ---

	[HttpPost("hire")]
	[RequirePermission(Permissions.StaffManage)]
	public async Task<IActionResult> HireStaff([FromBody] HireStaffDto request)
	{
		var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
		if (user == null) return NotFound("User must create a standard Voya account first.");

		var role = await _context.NexusRoles.FindAsync(request.RoleId);
		if (role == null) return BadRequest("Invalid Role ID.");

		user.NexusRoleId = role.Id;

		_context.SystemAuditLogs.Add(new SystemAuditLog
		{
			AdminUserId = GetUserId(),
			Action = "Hired Staff",
			EntityId = user.Id.ToString(),
			NewValue = $"Assigned Role: {role.Name}"
		});

		await _context.SaveChangesAsync();
		return Ok($"User {user.FullName} is now a {role.Name}.");
	}

	[HttpDelete("{userId}/fire")]
	[RequirePermission(Permissions.StaffManage)]
	public async Task<IActionResult> FireStaff(Guid userId)
	{
		var user = await _context.Users.FindAsync(userId);
		if (user == null) return NotFound();

		if (user.Id == GetUserId()) return BadRequest("You cannot fire yourself.");

		var oldRole = user.NexusRoleId;
		user.NexusRoleId = null;

		_context.SystemAuditLogs.Add(new SystemAuditLog
		{
			AdminUserId = GetUserId(),
			Action = "Fired Staff",
			EntityId = user.Id.ToString(),
			OldValue = oldRole.ToString() ?? "None"
		});

		await _context.SaveChangesAsync();
		return Ok("Staff access revoked.");
	}
}

// --- DTO CLASSES ---

public class HireStaffDto
{
	public string Email { get; set; } = string.Empty;
	public Guid RoleId { get; set; }
}

public class CreateRoleDto
{
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public List<string>? Permissions { get; set; } = new();
}