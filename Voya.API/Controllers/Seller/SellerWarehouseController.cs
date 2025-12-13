using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Seller;

[Authorize]
[ApiController]
[Route("api/v1/seller/warehouses")]
public class SellerWarehouseController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public SellerWarehouseController(VoyaDbContext context) { _context = context; }
	private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

	// 1. Manage Locations
	[HttpGet]
	public async Task<IActionResult> GetWarehouses()
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);
		return Ok(await _context.Warehouses.Where(w => w.StoreId == store!.Id).ToListAsync());
	}

	[HttpPost]
	public async Task<IActionResult> AddWarehouse([FromBody] Warehouse request)
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);

		request.StoreId = store!.Id;
		_context.Warehouses.Add(request);
		await _context.SaveChangesAsync();
		return Ok(request);
	}

	// 2. Manage Stock per Location
	[HttpPost("inventory/adjust")]
	public async Task<IActionResult> AdjustStock([FromBody] AdjustStockDto request)
	{
		var inv = await _context.ProductInventories
			.FirstOrDefaultAsync(i => i.ProductId == request.ProductId && i.WarehouseId == request.WarehouseId);

		if (inv == null)
		{
			inv = new ProductInventory
			{
				ProductId = request.ProductId,
				WarehouseId = request.WarehouseId,
				Quantity = 0
			};
			_context.ProductInventories.Add(inv);
		}

		inv.Quantity += request.QuantityChange; // Can be negative to remove stock

		// Sync Global Stock (Legacy Support)
		var product = await _context.Products.FindAsync(request.ProductId);
		if (product != null)
		{
			// Re-calculate total from all warehouses
			var allStock = await _context.ProductInventories
				.Where(i => i.ProductId == request.ProductId)
				.SumAsync(i => i.Quantity);
			// Add the new change (since db hasn't saved yet)
			product.StockQuantity = allStock + request.QuantityChange;
		}

		await _context.SaveChangesAsync();
		return Ok("Stock updated.");
	}
}

public class AdjustStockDto
{
	public Guid ProductId { get; set; }
	public Guid WarehouseId { get; set; }
	public int QuantityChange { get; set; }
}