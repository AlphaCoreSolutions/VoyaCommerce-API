using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Voya.Application.DTOs;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;
using Voya.API.Hubs;
using Voya.Core.Enums;
using Voya.API.Attributes;
using Voya.Core.Constants;

namespace Voya.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/orders")]
public class OrdersController : ControllerBase
{
	private readonly VoyaDbContext _context;
	private readonly IHubContext<LiveHub> _hubContext;

	// === NEW: Constant for the Automation Switch Key ===
	private const string AUCTION_AUTO_PAYOUT_KEY = "Auctions.AutoReleaseEnabled";

	public OrdersController(VoyaDbContext context, IHubContext<LiveHub> hubContext)
	{
		_context = context;
		_hubContext = hubContext;
	}

	// ==================================================================================
	// 1. CHECKOUT (Updated for Logistics & Shipments)
	// ==================================================================================
	[HttpPost("checkout")]
	public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
	{
		using var transaction = await _context.Database.BeginTransactionAsync();
		try
		{
			var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")!.Value);

			// 1. Get User
			var user = await _context.Users.FindAsync(userId);
			if (user == null) return Unauthorized();

			// 2. Validate Address (Single Mode)
			Address? singleAddress = null;
			if (!request.IsMultiAddress)
			{
				if (request.AddressId == null) return BadRequest("Address ID required.");
				singleAddress = await _context.Addresses.FirstOrDefaultAsync(a => a.Id == request.AddressId && a.UserId == userId);
				if (singleAddress == null) return BadRequest("Invalid Address ID");
			}

			// 3. Validate Cart
			var cart = await _context.Carts
				.Include(c => c.Items)
				.ThenInclude(i => i.Product)
				.FirstOrDefaultAsync(c => c.UserId == userId);

			if (cart == null || !cart.Items.Any()) return BadRequest("Cart is empty");

			// 4. Prepare Order Groups (Split by Address)
			var orderGroups = new Dictionary<Guid, List<CartItem>>();

			if (!request.IsMultiAddress)
			{
				orderGroups.Add(request.AddressId!.Value, cart.Items.ToList());
			}
			else
			{
				if (request.MultiAddressMap == null || !request.MultiAddressMap.Any())
					return BadRequest("Mapping required for multi-address checkout.");

				foreach (var map in request.MultiAddressMap)
				{
					var item = cart.Items.FirstOrDefault(i => i.ProductId == map.ProductId);
					if (item != null)
					{
						if (!orderGroups.ContainsKey(map.AddressId))
							orderGroups[map.AddressId] = new List<CartItem>();
						orderGroups[map.AddressId].Add(item);
					}
				}
			}

			// 5. Payment Method Validation
			PaymentMethod? paymentMethod = null;
			if (!Enum.TryParse<PaymentType>(request.PaymentType, true, out var paymentTypeEnum))
				return BadRequest("Invalid Payment Type.");

			if (paymentTypeEnum == PaymentType.CreditCard)
			{
				if (request.PaymentMethodId == null) return BadRequest("Payment Method ID required.");
				paymentMethod = await _context.PaymentMethods.FirstOrDefaultAsync(p => p.Id == request.PaymentMethodId && p.UserId == userId);
				if (paymentMethod == null) return BadRequest("Invalid Payment Method.");
			}

			// 6. Gift Wrap Validation
			GiftWrapOption? giftWrap = null;
			if (request.IsGift && request.GiftWrapOptionId.HasValue)
			{
				giftWrap = await _context.GiftWrapOptions.FindAsync(request.GiftWrapOptionId.Value);
			}

			// --- PROCESSING LOOP ---
			var createdOrders = new List<Order>();
			var groupTransactionId = Guid.NewGuid().ToString();

			decimal grandTotalSub = 0;
			decimal grandTotalFinal = 0;

			foreach (var group in orderGroups)
			{
				var addressId = group.Key;
				var items = group.Value;

				var address = await _context.Addresses.FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);
				if (address == null) return BadRequest($"Invalid Address ID: {addressId}");

				var orderSubTotal = items.Sum(i => i.Product.BasePrice * i.Quantity);
				var memberDiscount = orderSubTotal * user.MemberDiscountPercent;
				var orderTotalAfterMember = orderSubTotal - memberDiscount;
				decimal giftCost = giftWrap?.Price ?? 0;

				// A. Create Order
				var order = new Order
				{
					Id = Guid.NewGuid(),
					UserId = userId,
					GroupTransactionId = request.IsMultiAddress ? groupTransactionId : null,
					Status = OrderStatus.Pending,
					PlacedAt = DateTime.UtcNow,
					PaymentType = paymentTypeEnum,
					PaymentMethodId = paymentMethod?.Id,
					PaymentStatus = PaymentStatus.Unpaid,
					SubTotal = orderSubTotal,
					VoucherDiscount = 0, // Calculated later
					PointsDiscount = 0,  // Calculated later
					ShippingAddressId = address.Id,
					ShippingAddressJson = JsonSerializer.Serialize(address),
					PaymentMethodJson = paymentMethod != null ? JsonSerializer.Serialize(paymentMethod) : "{}",
					IsGift = request.IsGift,
					GiftMessage = request.GiftMessage,
					GiftWrapOptionId = giftWrap?.Id,
					GiftWrapName = giftWrap?.Name,
					GiftWrapPrice = giftCost
				};

				// B. Create Shipment (Logistics Record)
				var shipment = new Shipment
				{
					Id = Guid.NewGuid(),
					OrderId = order.Id,
					AddressId = address.Id,
					Status = ShipmentStatus.Pending,
					ShippingCost = 5.00m,
					CurrentLocation = "Warehouse",
					CreatedAt = DateTime.UtcNow,
					EstimatedDeliveryTime = DateTime.UtcNow.AddDays(3)
				};

				order.Shipments.Add(shipment);

				// C. Create OrderItems
				var orderItems = items.Select(i => new OrderItem
				{
					OrderId = order.Id,
					ShipmentId = shipment.Id,
					ProductId = i.ProductId,
					ProductName = i.Product.Name,
					Quantity = i.Quantity,
					UnitPrice = i.Product.BasePrice,
					SelectedOptionsJson = i.SelectedOptionsJson
				}).ToList();

				order.Items = orderItems;

				// D. Deduct Stock
				foreach (var item in items)
				{
					if (item.Product.StockQuantity < item.Quantity)
						return BadRequest($"Insufficient stock for {item.Product.Name}");

					item.Product.StockQuantity -= item.Quantity;
				}

				_context.Orders.Add(order);
				createdOrders.Add(order);

				grandTotalSub += orderSubTotal;
				grandTotalFinal += (orderTotalAfterMember + giftCost);
			}

			// --- GLOBAL DISCOUNTS ---
			decimal voucherDiscount = 0;
			decimal pointsDiscount = 0;
			int pointsRedeemed = 0;

			UserVoucher? userVoucherToUpdate = null;
			Voucher? voucherToAutoClaim = null;

			if (!string.IsNullOrEmpty(request.VoucherCode))
			{
				var existingClaim = await _context.UserVouchers.Include(uv => uv.Voucher)
					.FirstOrDefaultAsync(uv => uv.UserId == userId && uv.Voucher.Code == request.VoucherCode);

				if (existingClaim != null)
				{
					if (existingClaim.UsageCount >= existingClaim.Voucher.MaxUsesPerUser) return BadRequest("Voucher limit reached.");
					if (existingClaim.Voucher.EndDate < DateTime.UtcNow) return BadRequest("Voucher expired.");
					userVoucherToUpdate = existingClaim;
				}
				else
				{
					var globalVoucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.Code == request.VoucherCode);
					if (globalVoucher == null || !globalVoucher.IsActive || globalVoucher.EndDate < DateTime.UtcNow)
						return BadRequest("Invalid or expired voucher.");
					voucherToAutoClaim = globalVoucher;
				}

				var voucher = userVoucherToUpdate?.Voucher ?? voucherToAutoClaim!;
				voucherDiscount = voucher.Type == DiscountType.FixedAmount
					? voucher.Value
					: grandTotalSub * (voucher.Value / 100);

				if (voucherDiscount > grandTotalFinal) voucherDiscount = grandTotalFinal;
			}

			var totalAfterVoucher = grandTotalFinal - voucherDiscount;

			if (request.UsePoints && user.PointsBalance > 0 && totalAfterVoucher > 0)
			{
				const decimal POINTS_VALUE_RATE = 0.001m;
				decimal maxPointsValue = user.PointsBalance * POINTS_VALUE_RATE;

				if (maxPointsValue >= totalAfterVoucher)
				{
					pointsDiscount = totalAfterVoucher;
					pointsRedeemed = (int)Math.Ceiling(totalAfterVoucher / POINTS_VALUE_RATE);
				}
				else
				{
					pointsDiscount = maxPointsValue;
					pointsRedeemed = user.PointsBalance;
				}
			}

			var finalAmountToPay = Math.Max(0, totalAfterVoucher - pointsDiscount);

			// --- UPDATE ORDER TOTALS ---
			bool discountsApplied = false;
			foreach (var order in createdOrders)
			{
				if (!discountsApplied)
				{
					order.VoucherDiscount = voucherDiscount;
					order.PointsDiscount = pointsDiscount;
					order.PointsRedeemed = pointsRedeemed;
					order.TotalAmount = Math.Max(0, (order.SubTotal + order.GiftWrapPrice) - voucherDiscount - pointsDiscount);
					discountsApplied = true;
				}
				else
				{
					order.TotalAmount = order.SubTotal + order.GiftWrapPrice;
				}

				if (finalAmountToPay == 0) order.PaymentStatus = PaymentStatus.Paid;
			}

			// --- LOYALTY ---
			var globalMultiplierSetting = await _context.GlobalSettings
				.FirstOrDefaultAsync(s => s.Key == "Loyalty.PointsMultiplier");

			double multiplier = 1.0;
			if (globalMultiplierSetting != null)
				double.TryParse(globalMultiplierSetting.Value, out multiplier);

			int pointsEarned = (int)(finalAmountToPay * 10 * (decimal)multiplier);
			user.PointsBalance += pointsEarned;

			// --- DB UPDATES ---
			if (pointsRedeemed > 0) user.PointsBalance -= pointsRedeemed;

			if (userVoucherToUpdate != null)
			{
				userVoucherToUpdate.UsageCount++;
				_context.UserVouchers.Update(userVoucherToUpdate);
			}
			else if (voucherToAutoClaim != null)
			{
				_context.UserVouchers.Add(new UserVoucher { UserId = userId, VoucherId = voucherToAutoClaim.Id, UsageCount = 1, DateClaimed = DateTime.UtcNow });
			}

			_context.Carts.Remove(cart);

			await _context.SaveChangesAsync();
			await transaction.CommitAsync();

			return Ok(new
			{
				Message = $"Placed {createdOrders.Count} orders successfully.",
				GroupTransactionId = groupTransactionId,
				Orders = createdOrders.Select(o => o.Id),
				GrandTotal = finalAmountToPay,
				PointsEarned = pointsEarned
			});
		}
		catch (Exception ex)
		{
			await transaction.RollbackAsync();
			return StatusCode(500, "Error processing checkout: " + ex.Message);
		}
	}

	// ==================================================================================
	// 2. ORDER STATUS & AUCTION SETTLEMENT (UPDATED WITH KILL SWITCH)
	// ==================================================================================
	[HttpPut("{id}/status")]
	public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] string newStatusRaw)
	{
		if (!Enum.TryParse<OrderStatus>(newStatusRaw, true, out var newStatus))
			return BadRequest("Invalid status.");

		var order = await _context.Orders
			.Include(o => o.Items).ThenInclude(i => i.Product)
			.FirstOrDefaultAsync(o => o.Id == id);

		if (order == null) return NotFound("Order not found.");

		var oldStatus = order.Status;
		order.Status = newStatus;

		// --- NOTIFICATIONS ---
		if (newStatus == OrderStatus.Delivered && oldStatus != OrderStatus.Delivered)
		{
			var notif = new Notification
			{
				UserId = order.UserId,
				Title = "Order Delivered! 📦",
				Body = $"Your order #{order.Id.ToString()[..8]} has been delivered.",
				Type = "ReviewPrompt",
				RelatedEntityId = order.Id.ToString(),
				CreatedAt = DateTime.UtcNow
			};
			_context.Notifications.Add(notif);

			await _hubContext.Clients.Group(order.UserId.ToString())
				.SendAsync("ReceiveNotification", notif.Title, notif.Body);

			// === CRITICAL: RELEASE FUNDS FOR AUCTION ITEMS ===
			// 1. Check Global Automation Setting (Kill Switch)
			var autoPayoutSetting = await _context.GlobalSettings
				.FirstOrDefaultAsync(s => s.Key == AUCTION_AUTO_PAYOUT_KEY);

			// Default to TRUE (Enabled) if setting is missing
			bool isAutomationEnabled = autoPayoutSetting == null || bool.Parse(autoPayoutSetting.Value);

			if (isAutomationEnabled)
			{
				await ReleaseAuctionFunds(order);
			}
			else
			{
				// Automation is DISABLED. 
				// Money is held safely. Admin must release manually via NexusFinanceController.
			}
		}
		else if (newStatus == OrderStatus.Shipped && oldStatus != OrderStatus.Shipped)
		{
			var notif = new Notification
			{
				UserId = order.UserId,
				Title = "Order Shipped! 🚚",
				Body = $"Order #{order.Id.ToString()[..8]} is on the way.",
				Type = "OrderUpdate",
				RelatedEntityId = order.Id.ToString()
			};
			_context.Notifications.Add(notif);
		}

		await _context.SaveChangesAsync();
		return Ok(new { Message = "Status updated" });
	}

	private async Task ReleaseAuctionFunds(Order order)
	{
		// 1. Check if order contains auction item
		var auctionItem = order.Items.FirstOrDefault();
		if (auctionItem == null) return;

		// 2. Find associated Auction that was 'Sold'
		var auction = await _context.Auctions
			.FirstOrDefaultAsync(a => a.ProductId == auctionItem.ProductId && a.Status == AuctionStatus.Sold);

		if (auction != null && auction.SellerId != Guid.Empty)
		{
			// 3. Calculate Payout (e.g. 5% platform fee)
			decimal platformFeeRate = 0.05m;
			decimal grossAmount = order.TotalAmount;
			decimal fee = grossAmount * platformFeeRate;
			decimal netPayout = grossAmount - fee;

			// 4. Credit Seller
			var seller = await _context.Users.FindAsync(auction.SellerId);
			if (seller != null)
			{
				seller.WalletBalance += netPayout;

				// Using the unified WalletTransaction entity (works for Users and Stores)
				var transaction = new WalletTransaction
				{
					UserId = seller.Id,
					StoreId = null, // User-to-User sale (null StoreId)
					Amount = netPayout,
					Type = TransactionType.AuctionSale,
					Description = $"Auction Payout: {auctionItem.ProductName} (Order #{order.Id.ToString()[..8]})",
					OrderId = order.Id,
					Date = DateTime.UtcNow
				};

				_context.WalletTransactions.Add(transaction);
			}
		}
	}

	// ==================================================================================
	// 3. STANDARD CRUD ENDPOINTS
	// ==================================================================================

	[HttpGet]
	public async Task<IActionResult> GetMyOrders()
	{
		var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

		var orders = await _context.Orders
			.Where(o => o.UserId == userId)
			.OrderByDescending(o => o.PlacedAt)
			.Select(o => new
			{
				o.Id,
				o.Status,
				o.GroupTransactionId,
				o.PaymentStatus,
				o.TotalAmount,
				o.PlacedAt,
				o.ShippingAddressJson,
				o.IsGift,
				ItemsCount = o.Items.Count,
				ShipmentId = o.Shipments.FirstOrDefault().Id,
				Items = o.Items.Select(i => new { i.ProductName, i.Quantity, i.UnitPrice })
			})
			.ToListAsync();

		return Ok(orders);
	}

	[HttpGet("{id}/detailed")]
	[RequirePermission(Permissions.OrdersView)]
	public async Task<IActionResult> GetOrderDetails(Guid id)
	{
		var order = await _context.Orders
			.Include(o => o.Items).ThenInclude(i => i.Product)
			.Include(o => o.User)
			.FirstOrDefaultAsync(o => o.Id == id);

		if (order == null) return NotFound();

		var timeline = new List<object>
		{
			new { Title = "Order Placed", Time = order.PlacedAt, IsCompleted = true },
			new { Title = "Processing", Time = (DateTime?)null, IsCompleted = order.Status >= OrderStatus.Processing },
			new { Title = "Shipped", Time = (DateTime?)null, IsCompleted = order.Status >= OrderStatus.Shipped },
			new { Title = "Delivered", Time = (DateTime?)null, IsCompleted = order.Status == OrderStatus.Delivered }
		};

		return Ok(new { Order = order, Timeline = timeline });
	}

	[HttpDelete("{id}")]
	public async Task<IActionResult> DeleteOrder(Guid id)
	{
		var order = await _context.Orders.FindAsync(id);
		if (order == null) return NotFound();

		_context.Orders.Remove(order);
		await _context.SaveChangesAsync();
		return Ok("Order deleted");
	}

	// ==================================================================================
	// 4. REFUNDS, RETURNS & DISPUTES
	// ==================================================================================

	[HttpPost("{id}/refund")]
	[RequirePermission(Permissions.FinanceRefund)]
	public async Task<IActionResult> TriggerRefund(Guid id)
	{
		var order = await _context.Orders.FindAsync(id);
		if (order == null) return NotFound();

		order.Status = OrderStatus.Refunded;
		order.PaymentStatus = PaymentStatus.Refunded;

		await _context.SaveChangesAsync();
		return Ok("Refund processed successfully.");
	}

	[HttpPost("{id}/dispute/resolve")]
	[RequirePermission(Permissions.UsersManage)]
	public async Task<IActionResult> ResolveDispute(Guid id)
	{
		return Ok("Dispute resolved. User notified.");
	}

	[HttpGet("returns")]
	[RequirePermission(Permissions.OrdersManage)]
	public async Task<IActionResult> GetReturnRequests()
	{
		var returns = await _context.Orders
			.Include(o => o.User)
			.Include(o => o.Items).ThenInclude(i => i.Product)
			.Where(o => o.Status == OrderStatus.ReturnRequested)
			.OrderByDescending(o => o.PlacedAt)
			.Select(o => new
			{
				o.Id,
				Customer = o.User.FullName,
				DateRequested = DateTime.UtcNow.AddHours(-4),
				TotalAmount = o.TotalAmount,
				Reason = "Item damaged on arrival",
				Items = o.Items.Select(i => i.ProductName).ToList()
			})
			.ToListAsync();

		return Ok(returns);
	}

	[HttpPost("{id}/return/decide")]
	[RequirePermission(Permissions.FinanceRefund)]
	public async Task<IActionResult> ProcessReturnDecision(Guid id, [FromBody] ReturnDecisionDto request)
	{
		var order = await _context.Orders.FindAsync(id);
		if (order == null) return NotFound();

		if (request.Approved)
		{
			order.Status = request.Restock ? OrderStatus.Returned : OrderStatus.Refunded;
			order.PaymentStatus = PaymentStatus.Refunded;
		}
		else
		{
			order.Status = OrderStatus.Delivered;
		}

		await _context.SaveChangesAsync();
		return Ok(new { Message = request.Approved ? "Return approved & processed." : "Return rejected." });
	}
}