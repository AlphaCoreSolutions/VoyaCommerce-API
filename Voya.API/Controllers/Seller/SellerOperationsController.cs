using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Voya.Application.DTOs;
using Voya.Core.Entities;
using Voya.Core.Enums;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Seller;

[Authorize]
[ApiController]
[Route("api/v1/seller/operations")]
public class SellerOperationsController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public SellerOperationsController(VoyaDbContext context)
	{
		_context = context;
	}

	private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

	// --- FEATURE 3: RETURN MANAGER ---

	[HttpGet("returns")]
	public async Task<IActionResult> GetReturns()
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);

		var returns = await _context.ReturnRequests
			.Where(r => r.StoreId == store!.Id)
			.OrderByDescending(r => r.CreatedAt)
			.ToListAsync();

		return Ok(returns);
	}

	[HttpPost("returns/{id}/decide")]
	public async Task<IActionResult> DecideReturn(Guid id, [FromBody] ReturnDecisionDto request)
	{
		var r = await _context.ReturnRequests.FindAsync(id);
		if (r == null) return NotFound();

		r.Status = request.Approved ? ReturnStatus.Approved : ReturnStatus.Rejected;
		r.AdminNote = request.Note;

		// Log Audit
		// _auditService.Log(userId, $"{(request.Approved ? "Approved" : "Rejected")} return for Order {r.OrderId}");

		await _context.SaveChangesAsync();
		return Ok("Return status updated.");
	}

	// --- FEATURE 4: SHIPPING LABEL ---

	[HttpGet("shipping-label/{orderId}")]
	public IActionResult GenerateLabel(Guid orderId)
	{
		// In a real app, call DHL/FedEx API here.
		// For now, return a mock PDF URL.
		var mockUrl = $"https://api.voyacommerce.com/documents/labels/label_{orderId}.pdf";
		return Ok(new { LabelUrl = mockUrl, TrackingNumber = "TRK-" + new Random().Next(100000, 999999) });
	}

	// --- FEATURE 5: LOW STOCK ALERTS ---

	[HttpGet("low-stock")]
	public async Task<IActionResult> GetLowStockItems([FromQuery] int threshold = 5)
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);

		var items = await _context.Products
			.Where(p => p.StoreId == store!.Id && p.StockQuantity <= threshold)
			.Select(p => new { p.Id, p.Name, p.StockQuantity, p.MainImageUrl })
			.ToListAsync();

		return Ok(items);
	}
}

