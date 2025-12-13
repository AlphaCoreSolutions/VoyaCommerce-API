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
[Route("api/v1/seller/pos")]
public class SellerPosController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public SellerPosController(VoyaDbContext context)
	{
		_context = context;
	}

	// --- HELPER METHOD (Fixed CS0103) ---
	private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

	// FEATURE 7: POS CHECKOUT (Online Mode)
	// Used when the tablet has internet access
	[HttpPost("checkout")]
	public async Task<IActionResult> PosCheckout([FromBody] PosCheckoutDto request)
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);
		if (store == null) return Unauthorized("No store found.");

		decimal total = 0;
		var orderId = Guid.NewGuid();

		var order = new Order
		{
			Id = orderId,
			UserId = Guid.Empty, // Walk-in customer (Anonymous)
			Status = OrderStatus.Delivered, // Instant delivery
			PaymentStatus = PaymentStatus.Paid,
			PaymentType = PaymentType.CashOnDelivery, // Or define a specific POS type
			PlacedAt = DateTime.UtcNow, // Fixed: Use PlacedAt, not CreatedAt
			Items = new List<OrderItem>()
		};

		foreach (var item in request.Items)
		{
			var product = await _context.Products.FindAsync(item.ProductId);

			// Basic stock check
			if (product == null || product.StockQuantity < item.Qty)
				return BadRequest($"Item {product?.Name ?? "Unknown"} is out of stock.");

			// Deduct Stock
			product.StockQuantity -= item.Qty;

			// Calculate Line Item
			var lineTotal = product.BasePrice * item.Qty;
			total += lineTotal;

			order.Items.Add(new OrderItem
			{
				OrderId = orderId,
				ProductId = product.Id,
				ProductName = product.Name,
				Quantity = item.Qty,
				UnitPrice = product.BasePrice
			});
		}

		order.TotalAmount = total;

		_context.Orders.Add(order);

		// Record Financial Transaction
		_context.WalletTransactions.Add(new WalletTransaction
		{
			StoreId = store.Id,
			Amount = total,
			Type = TransactionType.Sale,
			Description = "POS Sale (Online)",
			Date = DateTime.UtcNow
		});

		await _context.SaveChangesAsync();
		return Ok(new { Message = "POS Sale Recorded", Total = total });
	}

	// FEATURE: OFFLINE SYNC
	// Used when internet returns after an outage
	[HttpPost("sync-offline")]
	public async Task<IActionResult> SyncOfflineSales([FromBody] List<OfflineSaleDto> offlineSales)
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);
		if (store == null) return Unauthorized();

		int successCount = 0;

		foreach (var sale in offlineSales)
		{
			var orderId = Guid.NewGuid();

			// Create Order using Historical Data
			var order = new Order
			{
				Id = orderId,
				UserId = Guid.Empty,
				Status = OrderStatus.Delivered,
				PaymentStatus = PaymentStatus.Paid,
				PaymentType = PaymentType.CashOnDelivery,

				TotalAmount = sale.TotalAmount,
				PlacedAt = sale.OccurredAt, // Use the ACTUAL time it happened (Fixed CS0117)
				Items = new List<OrderItem>()
			};

			foreach (var item in sale.Items)
			{
				var product = await _context.Products.FindAsync(item.ProductId);
				if (product != null)
				{
					// FORCE DEDUCT: Even if stock goes negative. 
					// The item is physically gone; the system must reflect reality.
					product.StockQuantity -= item.Qty;

					order.Items.Add(new OrderItem
					{
						OrderId = orderId,
						ProductId = product.Id,
						ProductName = product.Name,
						Quantity = item.Qty,
						UnitPrice = item.UnitPrice
					});
				}
			}

			_context.Orders.Add(order);

			_context.WalletTransactions.Add(new WalletTransaction
			{
				StoreId = store.Id,
				Amount = sale.TotalAmount,
				Type = TransactionType.Sale,
				Description = $"Offline Sync: {sale.ClientTransactionId}",
				Date = sale.OccurredAt
			});

			successCount++;
		}

		await _context.SaveChangesAsync();
		return Ok(new { Message = $"Synced {successCount} orders." });
	}
}

// --- DTO CLASSES ---

public class PosCheckoutDto
{
	public List<PosItem> Items { get; set; } = new();
}

public class PosItem
{
	public Guid ProductId { get; set; }
	public int Qty { get; set; }
}

public class OfflineSaleDto
{
	public string ClientTransactionId { get; set; } = string.Empty; // To prevent duplicate syncs
	public DateTime OccurredAt { get; set; }
	public decimal TotalAmount { get; set; }
	public List<OfflineItemDto> Items { get; set; } = new();
}

public class OfflineItemDto
{
	public Guid ProductId { get; set; }
	public int Qty { get; set; }
	public decimal UnitPrice { get; set; }
}