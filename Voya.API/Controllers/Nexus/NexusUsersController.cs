using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.API.Attributes;
using Voya.Core.Constants;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/users")]
public class NexusUsersController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public NexusUsersController(VoyaDbContext context) { _context = context; }

	// GET: api/v1/nexus/users/{id}/dna
	[HttpGet("{id}/dna")]
	[RequirePermission(Permissions.UsersView)]
	public async Task<IActionResult> GetUserDna(Guid id)
	{
		var user = await _context.Users
			.Include(u => u.Orders) // To calc stats
			.FirstOrDefaultAsync(u => u.Id == id);

		if (user == null) return NotFound();

		// Calculate Derived Stats
		var ltv = user.Orders.Where(o => o.PaymentStatus == Voya.Core.Entities.PaymentStatus.Paid).Sum(o => o.TotalAmount);
		var orderCount = user.Orders.Count;

		// Simple Risk Logic: Low Trust Score = High Risk
		var riskLevel = user.TrustScore < 50 ? "High" : (user.TrustScore < 80 ? "Medium" : "Low");

		return Ok(new
		{
			user.Id,
			user.FullName,
			user.Email,
			user.AvatarUrl,
			user.WalletBalance,
			user.PointsBalance,
			user.CurrentStreak,
			user.IsBanned,
			// Stats
			LTV = ltv,
			OrderCount = orderCount,
			RiskLevel = riskLevel,
			TrustScore = user.TrustScore
		});
	}

	// POST: api/v1/nexus/users/{id}/wallet/credit
	[HttpPost("{id}/wallet/credit")]
	[RequirePermission(Permissions.FinancePayout)] // Money involves higher permission
	public async Task<IActionResult> InjectCredit(Guid id, [FromBody] decimal amount)
	{
		var user = await _context.Users.FindAsync(id);
		if (user == null) return NotFound();

		user.WalletBalance += amount;
		await _context.SaveChangesAsync();

		// Log this action (Audit)
		// _auditService.Log("Wallet Injection", amount);

		return Ok(new { NewBalance = user.WalletBalance });
	}

	// POST: api/v1/nexus/users/{id}/ban
	[HttpPost("{id}/ban")]
	[RequirePermission(Permissions.UsersManage)]
	public async Task<IActionResult> ToggleBan(Guid id, [FromBody] BanRequestDto request)
	{
		var user = await _context.Users.FindAsync(id);
		if (user == null) return NotFound();

		user.IsBanned = request.IsBanned;
		user.BanReason = request.Reason;

		await _context.SaveChangesAsync();
		return Ok(new { Message = user.IsBanned ? "User Banned" : "User Unbanned" });
	}

	public class BanRequestDto { public bool IsBanned { get; set; } public string? Reason { get; set; } }
}