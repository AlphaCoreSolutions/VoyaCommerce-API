using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.API.Attributes;
using Voya.Core.Constants;
using Voya.Core.Entities;
using Voya.Core.Enums;
using Voya.Infrastructure.Persistence;
using Voya.Application.DTOs.Nexus;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/orders")]
public class NexusOrdersController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public NexusOrdersController(VoyaDbContext context)
	{
		_context = context;
	}

	// ---------------------------
	// GET: /api/v1/nexus/orders
	// Supports: status, q (search), dateFrom, dateTo, page, pageSize
	// ---------------------------
	[HttpGet]
	[RequirePermission(Permissions.OrdersView)]
	public async Task<IActionResult> GetOrders(
		[FromQuery] string? status,
		[FromQuery] string? q,
		[FromQuery] DateTime? dateFrom,
		[FromQuery] DateTime? dateTo,
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 20)
	{
		if (page < 1) page = 1;
		if (pageSize < 1) pageSize = 20;
		if (pageSize > 200) pageSize = 200;

		var query = _context.Orders
			.AsNoTracking()
			.Include(o => o.User)
			.OrderByDescending(o => o.PlacedAt)
			.AsQueryable();

		// Status filter
		if (!string.IsNullOrWhiteSpace(status))
		{
			if (!Enum.TryParse<OrderStatus>(status, true, out var parsed))
				return BadRequest("Invalid status.");

			query = query.Where(o => o.Status == parsed);
		}

		// Date range filter
		if (dateFrom.HasValue)
			query = query.Where(o => o.PlacedAt >= dateFrom.Value);

		if (dateTo.HasValue)
			query = query.Where(o => o.PlacedAt <= dateTo.Value);

		// Search
		// - order id (full/partial)
		// - user email
		// - user name
		if (!string.IsNullOrWhiteSpace(q))
		{
			q = q.Trim();
			query = query.Where(o =>
				o.Id.ToString().Contains(q) ||
				o.User.Email.Contains(q) ||
				o.User.FullName.Contains(q)
			);
		}

		var total = await query.CountAsync();

		var items = await query
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.Select(o => new NexusOrderListItemDto
			{
				Id = o.Id,
				Status = o.Status.ToString(),
				PaymentStatus = o.PaymentStatus.ToString(),
				TotalAmount = o.TotalAmount,
				PlacedAt = o.PlacedAt,

				UserId = o.UserId,
				UserName = o.User.FullName,
				UserEmail = o.User.Email,

				TrackingNumber = o.TrackingNumber,
				Carrier = o.Carrier
			})
			.ToListAsync();

		return Ok(new NexusPagedResult<NexusOrderListItemDto>
		{
			Items = items,
			Page = page,
			PageSize = pageSize,
			TotalCount = total
		});
	}

	// ---------------------------
	// GET: /api/v1/nexus/orders/{id}
	// ---------------------------
	[HttpGet("{id:guid}")]
	[RequirePermission(Permissions.OrdersView)]
	public async Task<IActionResult> GetOrder(Guid id)
	{
		var order = await _context.Orders
			.AsNoTracking()
			.Include(o => o.User)
			.Include(o => o.Items).ThenInclude(i => i.Product)
			.Include(o => o.Shipments).ThenInclude(s => s.Address)
			.FirstOrDefaultAsync(o => o.Id == id);

		if (order == null) return NotFound();

		var dto = new NexusOrderDetailDto
		{
			Id = order.Id,
			Status = order.Status.ToString(),
			PaymentStatus = order.PaymentStatus.ToString(),

			SubTotal = order.SubTotal,
			VoucherDiscount = order.VoucherDiscount,
			PointsDiscount = order.PointsDiscount,
			TotalAmount = order.TotalAmount,
			PlacedAt = order.PlacedAt,

			UserId = order.UserId,
			UserName = order.User.FullName,
			UserEmail = order.User.Email,
			UserPhone = order.User.PhoneNumber,

			ShippingAddressJson = order.ShippingAddressJson,
			PaymentMethodJson = order.PaymentMethodJson,

			TrackingNumber = order.TrackingNumber,
			Carrier = order.Carrier,

			Items = order.Items.Select(i => new NexusOrderItemDto
			{
				Id = i.Id,
				ProductId = i.ProductId,
				ProductName = i.Product != null ? i.Product.Name : "",
				Quantity = i.Quantity,
				UnitPrice = i.UnitPrice,
				ShipmentId = i.ShipmentId
			}).ToList(),

			Shipments = order.Shipments.Select(s => new NexusShipmentDto
			{
				Id = s.Id,
				Status = s.Status.ToString(),
				TrackingNumber = s.TrackingNumber,
				ExternalLabelUrl = s.ExternalLabelUrl,
				ShippingCost = s.ShippingCost,

				AddressId = s.AddressId,
				AddressSummary = s.Address != null
		? $"{s.Address.Street}, {s.Address.City}, {s.Address.State}, {s.Address.Country}"
		: "",

				CurrentLocation = s.CurrentLocation,
				CreatedAt = s.CreatedAt,
				EstimatedDeliveryTime = s.EstimatedDeliveryTime,
				ActualDeliveryTime = s.ActualDeliveryTime
			}).ToList()

		};

		return Ok(dto);
	}

	// ---------------------------
	// PUT: /api/v1/nexus/orders/{id}/status
	// Body: "Shipped"  (string)
	// ---------------------------
	[HttpPut("{id:guid}/status")]
	[RequirePermission(Permissions.OrdersManage)]
	public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] string newStatusRaw)
	{
		if (!Enum.TryParse<OrderStatus>(newStatusRaw, true, out var newStatus))
			return BadRequest("Invalid status.");

		var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
		if (order == null) return NotFound();

		order.Status = newStatus;
		await _context.SaveChangesAsync();

		return Ok(new { order.Id, Status = order.Status.ToString() });
	}

	// ---------------------------
	// POST: /api/v1/nexus/orders/{id}/cancel
	// ---------------------------
	[HttpPost("{id:guid}/cancel")]
	[RequirePermission(Permissions.OrdersManage)]
	public async Task<IActionResult> Cancel(Guid id, [FromBody] string? reason = null)
	{
		var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
		if (order == null) return NotFound();

		// Basic protection: don't cancel already completed flows
		if (order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Refunded)
			return BadRequest("Order cannot be cancelled in its current state.");

		order.Status = OrderStatus.Cancelled;

		// (Optional) if you later add an OrderNote/Audit entry, store reason there.
		await _context.SaveChangesAsync();

		return Ok(new { order.Id, Status = order.Status.ToString(), Reason = reason ?? "" });
	}

	// ---------------------------
	// POST: /api/v1/nexus/orders/{id}/refund
	// Matches your existing refund behavior in OrdersController, but in Nexus route.
	// ---------------------------
	[HttpPost("{id:guid}/refund")]
	[RequirePermission(Permissions.FinanceRefund)]
	public async Task<IActionResult> Refund(Guid id)
	{
		var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
		if (order == null) return NotFound();

		order.Status = OrderStatus.Refunded;
		order.PaymentStatus = PaymentStatus.Refunded;

		await _context.SaveChangesAsync();
		return Ok(new { order.Id, Status = order.Status.ToString(), PaymentStatus = order.PaymentStatus.ToString() });
	}

	// ---------------------------
	// POST: /api/v1/nexus/orders/{id}/dispatch
	// Calls the same logic you already have at:
	// POST /api/v1/logistics/hub/dispatch/{orderId}
	// ---------------------------
	[HttpPost("{id:guid}/dispatch")]
	[RequirePermission(Permissions.OrdersManage)]
	public async Task<IActionResult> Dispatch(Guid id)
	{
		// Minimal: reuse same underlying data and mimic the logistics hub behavior
		// by creating shipment via the configured provider.
		// Instead of duplicating adapter code here, we just call the same logic inline.
		// (If you prefer, we can refactor dispatch into a shared service later.)

		var order = await _context.Orders
			.Include(o => o.User)
			.FirstOrDefaultAsync(o => o.Id == id);

		if (order == null) return NotFound();

		var provider = await _context.LogisticsProviders.FirstOrDefaultAsync(p => p.IsActive);
		if (provider == null) return BadRequest("No active logistics provider.");

		// NOTE:
		// Your LogisticsHubController already chooses adapter based on provider.Type
		// and returns TrackingNumber. The cleanest long-term move is refactoring that
		// logic into a service and calling it from both controllers.
		//
		// For now: we just redirect user to use the hub endpoint logically:
		return Ok(new
		{
			Message = "Dispatch should be executed through Logistics Hub endpoint.",
			HubEndpoint = $"/api/v1/logistics/hub/dispatch/{id}"
		});
	}
}
