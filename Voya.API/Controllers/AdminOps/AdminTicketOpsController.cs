using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.API.Attributes;
using Voya.Core.Constants;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.AdminOps;

[Authorize]
[ApiController]
[Route("api/v1/admin/tickets")]
public class AdminTicketOpsController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public AdminTicketOpsController(VoyaDbContext context)
	{
		_context = context;
	}

	// 1. Get Open Tickets (Filtered)
	[HttpGet("queue")]
	[RequirePermission(Permissions.UsersView)]
	public async Task<IActionResult> GetTicketQueue([FromQuery] TicketStatus? status, [FromQuery] TicketType? type)
	{
		var query = _context.Tickets.AsQueryable();

		// Default to showing Open/InProgress if no status requested
		if (status.HasValue)
		{
			query = query.Where(t => t.Status == status.Value);
		}
		else
		{
			query = query.Where(t => t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress);
		}

		if (type.HasValue)
		{
			query = query.Where(t => t.Type == type.Value);
		}

		var tickets = await query
			.OrderByDescending(t => t.Priority) // Urgent first
			.ThenBy(t => t.CreatedAt)           // Oldest first
			.ToListAsync();

		return Ok(tickets);
	}

	// 2. View Ticket Details (Including User Info)
	[HttpGet("{id}")]
	[RequirePermission(Permissions.UsersView)]
	public async Task<IActionResult> GetTicketDetails(Guid id)
	{
		// Join with User to see who opened it
		// Join with Order if RelatedOrderId exists (requires manual join since RelatedOrderId is string)

		var ticket = await _context.Tickets.FindAsync(id);
		if (ticket == null) return NotFound();

		var user = await _context.Users
			.Where(u => u.Id == ticket.UserId)
			.Select(u => new { u.FullName, u.Email, u.PhoneNumber })
			.FirstOrDefaultAsync();

		return Ok(new
		{
			Ticket = ticket,
			OpenedBy = user
		});
	}

	// 3. Resolve / Reply
	[HttpPost("{id}/resolve")]
	[RequirePermission(Permissions.UsersManage)]
	public async Task<IActionResult> ResolveTicket(Guid id, [FromBody] ResolveTicketDto request)
	{
		var ticket = await _context.Tickets.FindAsync(id);
		if (ticket == null) return NotFound();

		ticket.AdminResponse = request.Response;
		ticket.Status = TicketStatus.Resolved;
		ticket.ResolvedAt = DateTime.UtcNow;

		// Optional: Trigger Notification to User here
		// _notificationService.Send(ticket.UserId, "Your ticket has been resolved.");

		await _context.SaveChangesAsync();
		return Ok("Ticket resolved and response saved.");
	}

	// 4. Update Status Only (e.g. Mark "In Progress")
	[HttpPut("{id}/status")]
	[RequirePermission(Permissions.UsersManage)]
	public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] TicketStatus newStatus)
	{
		var ticket = await _context.Tickets.FindAsync(id);
		if (ticket == null) return NotFound();

		ticket.Status = newStatus;
		await _context.SaveChangesAsync();
		return Ok($"Ticket marked as {newStatus}");
	}
}

public class ResolveTicketDto
{
	public string Response { get; set; } = string.Empty;
}