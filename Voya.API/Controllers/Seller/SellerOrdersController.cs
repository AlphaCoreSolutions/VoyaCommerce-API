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
[Route("api/v1/seller/orders")]
public class SellerOrdersController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public SellerOrdersController(VoyaDbContext context)
	{
		_context = context;
	}

	private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

	// 1. GET INCOMING ORDERS
	[HttpGet]
	public async Task<IActionResult> GetStoreOrders([FromQuery] string status = "all")
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);
		if (store == null) return Unauthorized("No store found.");

		// Logic: Find OrderItems linked to this Store's products, then grab the Order.
		// We group by OrderId so if an order has 2 items from this store, it shows once.
		var query = _context.OrderItems
			.Include(i => i.Product)
			.Include(i => i.Order)
			.ThenInclude(o => o.User) // To show customer name
			.Where(i => i.Product!.StoreId == store.Id);

		// Optional Status Filter
		if (status.ToLower() != "all" && Enum.TryParse<OrderStatus>(status, true, out var statusEnum))
		{
			query = query.Where(i => i.Order.Status == statusEnum);
		}

		var orders = await query
			.OrderByDescending(i => i.Order.PlacedAt)
			.Select(i => i.Order)
			.Distinct() // Important!
			.Select(o => new
			{
				o.Id,
				CustomerName = o.User.FullName,
				o.PlacedAt,
				o.Status,
				o.TotalAmount, // Note: This is order total, not just store total
							   // Only show items belonging to THIS store
				MyItems = o.Items.Where(item => item.Product!.StoreId == store.Id).Select(item => new
				{
					item.ProductName,
					item.Quantity,
					item.UnitPrice,
					item.SelectedOptionsJson,
					item.LineTotal
				})
			})
			.ToListAsync();

		return Ok(orders);
	}

	// 2. GET ORDER DETAILS (Specific to Seller)
	[HttpGet("{orderId}")]
	public async Task<IActionResult> GetOrderDetails(Guid orderId)
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);
		if (store == null) return Unauthorized();

		var order = await _context.Orders
			.Include(o => o.User)
			.Include(o => o.Items)
			.ThenInclude(i => i.Product)
			.FirstOrDefaultAsync(o => o.Id == orderId);

		if (order == null) return NotFound();

		// Security check: Does this order actually contain my products?
		var myItems = order.Items.Where(i => i.Product!.StoreId == store.Id).ToList();
		if (!myItems.Any()) return Forbid("This order does not contain items from your store.");

		return Ok(new
		{
			order.Id,
			order.Status,
			order.PlacedAt,
			Customer = new
			{
				order.User.FullName,
				order.User.Email,
				// Don't expose phone unless necessary
			},
			ShippingAddress = order.ShippingAddressJson, // Seller needs this to ship!
			ItemsToFulfill = myItems.Select(i => new
			{
				i.ProductName,
				i.Quantity,
				i.SelectedOptionsJson,
				Image = i.Product!.MainImageUrl
			})
		});
	}

	// 3. UPDATE ORDER STATUS (e.g. Mark as Shipped)
	[HttpPut("{orderId}/status")]
	public async Task<IActionResult> UpdateOrderStatus(Guid orderId, [FromBody] UpdateOrderStatusDto request)
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);

		var order = await _context.Orders
			.Include(o => o.Items)
			.ThenInclude(i => i.Product)
			.FirstOrDefaultAsync(o => o.Id == orderId);

		if (order == null) return NotFound();

		// Verify ownership
		if (!order.Items.Any(i => i.Product!.StoreId == store!.Id))
			return Forbid();

		// In a complex app, we'd only update the specific ITEM status.
		// For this version, we assume the Seller controls the Order status.
		order.Status = request.NewStatus;

		// Logic: If marking delivered, maybe set Activation date or release funds?

		await _context.SaveChangesAsync();
		return Ok(new { Message = $"Order marked as {request.NewStatus}" });
	}
}

public class UpdateOrderStatusDto
{
	public OrderStatus NewStatus { get; set; }
}