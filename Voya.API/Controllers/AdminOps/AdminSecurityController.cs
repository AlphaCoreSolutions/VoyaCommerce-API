using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.API.Attributes;
using Voya.Core.Constants;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.AdminOps;

[Authorize]
[ApiController]
[Route("api/v1/admin/security")]
public class AdminSecurityController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public AdminSecurityController(VoyaDbContext context) { _context = context; }

	// FEATURE 1: DUPLICATE ACCOUNT DETECTOR
	[HttpGet("duplicates")]
	[RequirePermission(Permissions.UsersView)]
	public async Task<IActionResult> FindDuplicates()
	{
		// Finds users sharing the same Phone Number but different Emails
		var duplicates = await _context.Users
			.GroupBy(u => u.PhoneNumber)
			.Where(g => g.Count() > 1)
			.Select(g => new { PhoneNumber = g.Key, Accounts = g.Select(u => u.Email).ToList() })
			.ToListAsync();

		return Ok(duplicates);
	}

	// FEATURE 9: USER TRUST SCORE CALCULATOR
	[HttpPost("trust-score/{userId}")]
	[RequirePermission(Permissions.UsersManage)]
	public async Task<IActionResult> RecalculateTrust(Guid userId)
	{
		var user = await _context.Users.FindAsync(userId);
		if (user == null) return NotFound();

		var orders = await _context.Orders.Where(o => o.UserId == userId).ToListAsync();
		if (!orders.Any()) return Ok("No orders to calculate.");

		double completed = orders.Count(o => o.Status == OrderStatus.Delivered);
		double total = orders.Count;

		// Simple Logic: % of successful orders * 100
		double score = (completed / total) * 100;

		user.TrustScore = score;
		await _context.SaveChangesAsync();

		return Ok(new { NewScore = score });
	}

	// FEATURE 4: READ-ONLY ADMIN (Conceptual)
	// To implement Read-Only, you simply assign a Role using NexusStaffController
	// that contains ONLY ".view" permissions (e.g., "users.view", "stores.view")
	// and NO ".manage" permissions. The [RequirePermission] attribute handles the rest automatically.
}