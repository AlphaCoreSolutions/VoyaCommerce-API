using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.API.Attributes;
using Voya.Core.Constants;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/tickets")]
public class NexusTicketsController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public NexusTicketsController(VoyaDbContext context) { _context = context; }

	// GET: List Tickets (Inbox)
	[HttpGet]
	[RequirePermission(Permissions.UsersView)]
	public async Task<IActionResult> GetTickets([FromQuery] string? status)
	{
		var query = _context.Tickets.AsQueryable();

		if (!string.IsNullOrEmpty(status) && Enum.TryParse<TicketStatus>(status, true, out var statusEnum))
		{
			query = query.Where(t => t.Status == statusEnum);
		}

		var list = await query
			.OrderByDescending(t => t.Priority) // Urgent first
			.ThenBy(t => t.CreatedAt)
			.Select(t => new
			{
				t.Id,
				t.Subject,
				Status = t.Status.ToString(),
				Priority = t.Priority.ToString(),
				Date = t.CreatedAt,
				User = "User #" + t.UserId.ToString().Substring(0, 4) // Mock user name join
			})
			.ToListAsync();

		return Ok(list);
	}

	// GET: Detail with Messages
	[HttpGet("{id}")]
	[RequirePermission(Permissions.UsersView)]
	public async Task<IActionResult> GetTicketDetails(Guid id)
	{
		var ticket = await _context.Tickets.FindAsync(id);
		if (ticket == null) return NotFound();

		var messages = await _context.TicketMessages
			.Where(m => m.TicketId == id)
			.OrderBy(m => m.Timestamp)
			.ToListAsync();

		return Ok(new { Ticket = ticket, Messages = messages });
	}

	// POST: Send Reply
	[HttpPost("{id}/reply")]
	[RequirePermission(Permissions.UsersManage)]
	public async Task<IActionResult> ReplyToTicket(Guid id, [FromBody] string message)
	{
		var ticket = await _context.Tickets.FindAsync(id);
		if (ticket == null) return NotFound();

		var msg = new TicketMessage
		{
			TicketId = id,
			Message = message,
			IsAdminReply = true,
			SenderName = User.Identity?.Name ?? "Support Agent"
		};

		_context.TicketMessages.Add(msg);

		// Auto-update status if it was Open
		if (ticket.Status == TicketStatus.Open) ticket.Status = TicketStatus.InProgress;

		await _context.SaveChangesAsync();
		return Ok(msg);
	}

	// PUT: Update Status
	[HttpPut("{id}/status")]
	[RequirePermission(Permissions.UsersManage)]
	public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] string status)
	{
		if (!Enum.TryParse<TicketStatus>(status, true, out var newStatus))
			return BadRequest("Invalid Status");

		var ticket = await _context.Tickets.FindAsync(id);
		if (ticket == null) return NotFound();

		ticket.Status = newStatus;
		if (newStatus == TicketStatus.Resolved || newStatus == TicketStatus.Closed)
			ticket.ResolvedAt = DateTime.UtcNow;

		await _context.SaveChangesAsync();
		return Ok("Status updated.");
	}
}