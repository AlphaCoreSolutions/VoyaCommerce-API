using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.API.Attributes;
using Voya.Core.Constants;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.AdminOps;

[Authorize]
[ApiController]
[Route("api/v1/admin/users")]
public class AdminUserOpsController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public AdminUserOpsController(VoyaDbContext context) { _context = context; }

	// 1. Search Users
	[HttpGet("search")]
	[RequirePermission(Permissions.UsersView)]
	public async Task<IActionResult> SearchUsers([FromQuery] string query)
	{
		var users = await _context.Users
			.Where(u => u.Email.Contains(query) || u.FullName.Contains(query) || u.PhoneNumber.Contains(query))
			.Take(20)
			.Select(u => new { u.Id, u.FullName, u.Email, u.IsBanned, u.CreatedAt })
			.ToListAsync();
		return Ok(users);
	}

	// 2. Ban/Unban
	[HttpPost("{userId}/ban")]
	[RequirePermission(Permissions.UsersManage)]
	public async Task<IActionResult> ToggleBan(Guid userId, [FromBody] DecisionDto request)
	{
		var user = await _context.Users.FindAsync(userId);
		if (user == null) return NotFound();

		// Prevent banning other admins
		if (user.NexusRoleId != null) return BadRequest("Cannot ban a staff member. Fire them first.");

		// If Approved=True, we interpret as "Ban this user"
		user.IsBanned = request.Approved;
		user.BanReason = request.Approved ? request.Reason : null;

		await _context.SaveChangesAsync();
		return Ok($"User banned status: {user.IsBanned}");
	}
}