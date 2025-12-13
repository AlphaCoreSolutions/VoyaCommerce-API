using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

[Authorize]
[ApiController]
[Route("api/v1/seller/smart")]
public class SellerSmartController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public SellerSmartController(VoyaDbContext context) { _context = context; }
	private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

	// --- FEATURE: SMART REPLENISHMENT ---
	[HttpGet("replenishment")]
	public async Task<IActionResult> GetReplenishmentAdvice()
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);
		var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

		// 1. Get Sales History
		var salesData = await _context.OrderItems
			.Include(i => i.Product)
			.Include(i => i.Order)
			.Where(i => i.Product.StoreId == store!.Id && i.Order.PlacedAt > thirtyDaysAgo)
			.GroupBy(i => new { i.ProductId, i.ProductName, i.Product.StockQuantity })
			.Select(g => new
			{
				g.Key.ProductName,
				CurrentStock = g.Key.StockQuantity,
				TotalSold30Days = g.Sum(x => x.Quantity)
			})
			.ToListAsync();

		var advice = new List<object>();

		foreach (var item in salesData)
		{
			if (item.TotalSold30Days > 0)
			{
				double avgDaily = (double)item.TotalSold30Days / 30.0;
				double daysLeft = item.CurrentStock / avgDaily;

				if (daysLeft < 7) // Critical if less than 1 week stock remains
				{
					advice.Add(new
					{
						Product = item.ProductName,
						DaysRemaining = Math.Round(daysLeft, 1),
						Message = $"Reorder now! You will verify run out in {Math.Round(daysLeft)} days."
					});
				}
			}
		}

		return Ok(advice);
	}

	// --- FEATURE: CRM SEGMENTS ---
	[HttpPost("crm/analyze")]
	public async Task<IActionResult> AnalyzeCustomerSegments()
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);

		// 1. Find Big Spenders (> $500 total)
		var bigSpenders = await _context.Orders
			.Include(o => o.Items)
			.Where(o => o.Items.Any(i => i.Product.StoreId == store.Id))
			.GroupBy(o => o.UserId)
			.Select(g => new { UserId = g.Key, TotalSpent = g.Sum(o => o.TotalAmount) })
			.Where(x => x.TotalSpent > 500)
			.ToListAsync();

		int count = 0;
		foreach (var spender in bigSpenders)
		{
			if (!await _context.CustomerTags.AnyAsync(t => t.CustomerUserId == spender.UserId && t.Tag == "Big Spender"))
			{
				_context.CustomerTags.Add(new CustomerTag { StoreId = store!.Id, CustomerUserId = spender.UserId, Tag = "Big Spender" });
				count++;
			}
		}

		await _context.SaveChangesAsync();
		return Ok($"Analysis complete. Tagged {count} new Big Spenders.");
	}

	// --- FEATURE: COMPETITOR ALERTS ---
	[HttpGet("competitor-alerts")]
	public async Task<IActionResult> GetCompetitorAlerts()
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);
		return Ok(await _context.CompetitorAlerts.Where(c => c.StoreId == store!.Id && !c.IsResolved).ToListAsync());
	}
}