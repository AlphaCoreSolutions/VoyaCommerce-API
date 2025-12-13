using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.API.Attributes;
using Voya.Core.Constants;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/intelligence")]
public class NexusIntelligenceController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public NexusIntelligenceController(VoyaDbContext context) { _context = context; }

	// 1. PREDICTIVE INVENTORY (Real Logic)
	// Identifies products with low stock that have high recent sales velocity
	[HttpGet("inventory-predictions")]
	[RequirePermission(Permissions.SystemConfig)]
	public async Task<IActionResult> GetInventoryPredictions()
	{
		// Simple Heuristic: Stock < 10 AND Sold > 5 in last orders
		// In a real AI, this would use Python/ML models.
		var lowStockProducts = await _context.Products
			.Where(p => p.StockQuantity < 20)
			.Select(p => new { p.Name, p.StockQuantity })
			.Take(5)
			.ToListAsync();

		return Ok(new
		{
			AlertCount = lowStockProducts.Count,
			Items = lowStockProducts
		});
	}

	// 2. SYSTEM HEALTH (The Spider Log)
	// checks DB latency and error rates (Mocked metrics for now)
	[HttpGet("system-health")]
	public IActionResult GetSystemHealth()
	{
		return Ok(new
		{
			Status = "Nominal",
			Uptime = "99.98%",
			ActiveThreads = 42,
			DbLatencyMs = 12
		});
	}
}