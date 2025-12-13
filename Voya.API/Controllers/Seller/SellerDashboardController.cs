using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Voya.Core.Enums;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Seller;

[Authorize]
[ApiController]
[Route("api/v1/seller/dashboard")]
public class SellerDashboardController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public SellerDashboardController(VoyaDbContext context)
	{
		_context = context;
	}

	private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

	[HttpGet]
	public async Task<IActionResult> GetDashboardStats()
	{
		var userId = GetUserId();

		// 1. Find the User's Store
		var store = await _context.Stores
			.Include(s => s.Products)
			.FirstOrDefaultAsync(s => s.OwnerId == userId);

		if (store == null) return BadRequest("You do not have a store yet.");
		if (store.Status != StoreStatus.Active) return BadRequest("Store is not active yet.");

		// 2. Calculate Stats
		// Note: Ideally, 'Orders' would be linked to 'Store', but currently they link to 'User'. 
		// We will query Orders containing products from this store.

		var storeProductIds = store.Products.Select(p => p.Id).ToList();

		var recentOrderItems = await _context.Orders
			.SelectMany(o => o.Items)
			.Where(i => storeProductIds.Contains(i.ProductId))
			.Include(i => i.Order)
			.OrderByDescending(i => i.Order.PlacedAt)
			.Take(5)
			.ToListAsync();

		var totalSales = store.Products.Sum(p => p.StockQuantity); // Placeholder logic
																   // Real logic: Query OrderItems table for sum of line totals for this store's products

		return Ok(new
		{
			StoreName = store.Name,
			TotalRevenue = store.TotalRevenue, // You should update this when orders complete
			TotalProducts = store.Products.Count,
			TotalOrders = store.TotalSales,
			RecentSales = recentOrderItems.Select(i => new
			{
				ProductName = i.ProductName,
				Price = i.LineTotal,
				Date = i.Order.PlacedAt,
				Status = i.Order.Status.ToString()
			})
		});
	}
}