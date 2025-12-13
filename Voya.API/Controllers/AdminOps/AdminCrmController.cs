using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Voya.API.Attributes;
using Voya.Core.Constants;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.AdminOps;

[Authorize]
[ApiController]
[Route("api/v1/admin/crm")]
public class AdminCrmController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public AdminCrmController(VoyaDbContext context) { _context = context; }
	private Guid GetAdminId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

	// FEATURE 3: INTERNAL NOTES
	[HttpPost("notes")]
	public async Task<IActionResult> AddNote([FromBody] AdminInternalNote note)
	{
		note.WrittenByAdminId = GetAdminId();
		_context.AdminInternalNotes.Add(note);
		await _context.SaveChangesAsync();
		return Ok("Note added.");
	}

	[HttpGet("notes/{targetId}")]
	public async Task<IActionResult> GetNotes(string targetId)
	{
		return Ok(await _context.AdminInternalNotes
			.Where(n => n.TargetEntityId == targetId)
			.OrderByDescending(n => n.CreatedAt)
			.ToListAsync());
	}

	// FEATURE 4: USER JOURNEY TIMELINE
	[HttpGet("users/{userId}/journey")]
	public async Task<IActionResult> GetUserJourney(Guid userId)
	{
		// 1. Get Orders
		var orders = await _context.Orders
			.Where(o => o.UserId == userId)
			.Select(o => new { Type = "Order", Date = o.PlacedAt, Details = $"Placed Order {o.TotalAmount}" })
			.ToListAsync();

		// 2. Get Tickets
		var tickets = await _context.Tickets
			.Where(t => t.UserId == userId)
			.Select(t => new { Type = "Ticket", Date = t.CreatedAt, Details = $"Opened Ticket: {t.Subject}" })
			.ToListAsync();

		// 3. Get Reviews
		var reviews = await _context.Reviews
			.Where(r => r.UserId == userId)
			.Select(r => new { Type = "Review", Date = r.CreatedAt, Details = $"Rated Product {r.Rating} stars" })
			.ToListAsync();

		// Combine and Sort
		var timeline = orders.Concat(tickets).Concat(reviews).OrderByDescending(x => x.Date).ToList();
		return Ok(timeline);
	}

	// FEATURE 6: REFUND RISK ANALYSIS
	[HttpGet("users/{userId}/risk")]
	public async Task<IActionResult> GetRefundRisk(Guid userId)
	{
		var totalOrders = await _context.Orders.CountAsync(o => o.UserId == userId);
		if (totalOrders == 0) return Ok(new { Risk = "None", Score = 0 });

		var refunds = await _context.ReturnRequests.CountAsync(r => r.UserId == userId);

		double ratio = (double)refunds / totalOrders;
		string riskLevel = ratio > 0.5 ? "High" : (ratio > 0.2 ? "Medium" : "Low");

		return Ok(new
		{
			RiskLevel = riskLevel,
			RefundRatio = Math.Round(ratio * 100, 1) + "%",
			TotalOrders = totalOrders,
			TotalReturns = refunds
		});
	}
}