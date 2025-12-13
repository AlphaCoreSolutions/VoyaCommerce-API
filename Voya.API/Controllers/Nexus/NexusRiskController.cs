using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/risk")]
public class NexusRiskController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public NexusRiskController(VoyaDbContext context) { _context = context; }

	// FEATURE 1: AI FRAUD DETECTION (REAL LOGIC)
	[HttpPost("scan/{orderId}")]
	public async Task<IActionResult> ScanOrder(Guid orderId)
	{
		var order = await _context.Orders
			.Include(o => o.User)
			.FirstOrDefaultAsync(o => o.Id == orderId);

		if (order == null) return NotFound();

		double riskScore = 0;
		var reasons = new List<string>();

		// 1. Check Total Value
		if (order.TotalAmount > 1000)
		{
			riskScore += 20;
			reasons.Add("High Value (> $1000)");
		}

		// 2. Check Order Velocity (Did they buy recently?)
		var oneHourAgo = DateTime.UtcNow.AddHours(-1);
		var recentOrdersCount = await _context.Orders
			.CountAsync(o => o.UserId == order.UserId && o.PlacedAt > oneHourAgo && o.Id != orderId);

		if (recentOrdersCount >= 3)
		{
			riskScore += 50;
			reasons.Add($"High Velocity ({recentOrdersCount} orders in 1hr)");
		}

		// 3. Check Account Age
		if (order.User.CreatedAt > DateTime.UtcNow.AddHours(-24))
		{
			riskScore += 30;
			reasons.Add("New Account (< 24h)");
		}

		// 4. Save Alert if Risk > 50
		bool isRisky = riskScore > 50;

		if (isRisky)
		{
			var alert = new FraudAlert
			{
				OrderId = order.Id,
				UserId = order.UserId,
				RiskScore = riskScore,
				Level = riskScore > 80 ? FraudRiskLevel.Critical : FraudRiskLevel.High,
				Reason = string.Join(", ", reasons)
			};
			_context.FraudAlerts.Add(alert);
			await _context.SaveChangesAsync();
		}

		return Ok(new
		{
			IsRisky = isRisky,
			Score = riskScore,
			Breakdown = reasons
		});
	}

	[HttpGet("alerts")]
	public async Task<IActionResult> GetActiveAlerts()
	{
		return Ok(await _context.FraudAlerts.Where(f => !f.IsResolved).ToListAsync());
	}
}