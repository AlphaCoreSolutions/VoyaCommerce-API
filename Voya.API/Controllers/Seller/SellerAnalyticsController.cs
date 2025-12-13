using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Voya.Core.Entities;
using Voya.Core.Enums;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Seller;

[Authorize]
[ApiController]
[Route("api/v1/seller/analytics")]
public class SellerAnalyticsController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public SellerAnalyticsController(VoyaDbContext context)
	{
		_context = context;
	}

	private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

	[HttpGet("overview")]
	public async Task<IActionResult> GetBusinessOverview([FromQuery] string period = "30d")
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);
		if (store == null) return Unauthorized();

		var startDate = period == "7d" ? DateTime.UtcNow.AddDays(-7) : DateTime.UtcNow.AddDays(-30);

		// Fetch Orders for this store within period
		var orders = await _context.Orders
			.Include(o => o.Items)
			.ThenInclude(i => i.Product)
			.Where(o => o.PlacedAt >= startDate && o.Status != OrderStatus.Cancelled)
			.ToListAsync();

		// Filter items specific to this store
		var storeItems = orders.SelectMany(o => o.Items)
			.Where(i => i.Product?.StoreId == store.Id);

		var totalRevenue = storeItems.Sum(i => i.LineTotal);
		var totalOrders = orders.Count(o => o.Items.Any(i => i.Product?.StoreId == store.Id));

		// Simple "Repeat Customer" Logic: Count distinct UserIds who bought > 1 time
		var repeatCustomers = orders
			.GroupBy(o => o.UserId)
			.Count(g => g.Count() > 1);

		return Ok(new
		{
			Period = period,
			Revenue = totalRevenue,
			Orders = totalOrders,
			RepeatCustomers = repeatCustomers,
			ConversionRate = 3.5 // Hardcoded for now (needs View tracking middleware)
		});
	}

	[HttpGet("regional")]
	public async Task<IActionResult> GetSalesByRegion()
	{
		// Mock data structure for the heatmap
		return Ok(new List<object>
		{
			new { City = "Amman", Sales = 15000 },
			new { City = "Irbid", Sales = 4500 },
			new { City = "Aqaba", Sales = 2100 }
		});
	}
}