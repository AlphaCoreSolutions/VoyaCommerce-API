using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.Application.DTOs;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/tickets")]
public class TicketsController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public TicketsController(VoyaDbContext context)
	{
		_context = context;
	}

	private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")!.Value);

	[HttpPost]
	public async Task<IActionResult> CreateTicket(CreateTicketRequest request)
	{
		var userId = GetUserId();

		if (!Enum.TryParse<TicketType>(request.Type, true, out var type))
			return BadRequest("Invalid Ticket Type");

		if (!Enum.TryParse<TicketPriority>(request.Priority, true, out var priority))
			priority = TicketPriority.Medium; // Default

		var ticket = new Ticket
		{
			UserId = userId,
			Subject = request.Subject,
			Description = request.Description,
			Type = type,
			Priority = priority,
			RelatedOrderId = request.OrderId
		};

		_context.Tickets.Add(ticket);
		await _context.SaveChangesAsync();

		return Ok(new { Message = "Ticket created successfully", TicketId = ticket.Id });
	}

	[HttpGet]
	public async Task<ActionResult<List<TicketDto>>> GetMyTickets()
	{
		var userId = GetUserId();
		var tickets = await _context.Tickets
			.Where(t => t.UserId == userId)
			.OrderByDescending(t => t.CreatedAt)
			.Select(t => new TicketDto(
				t.Id, t.Subject, t.Status.ToString(), t.CreatedAt.ToString("g"), t.AdminResponse))
			.ToListAsync();

		return Ok(tickets);
	}

	// Put endpoint to close ticket (User side)
	[HttpPut("{id}/close")]
	public async Task<IActionResult> CloseTicket(Guid id)
	{
		var userId = GetUserId();
		var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

		if (ticket == null) return NotFound();

		ticket.Status = TicketStatus.Closed;
		ticket.ResolvedAt = DateTime.UtcNow;
		await _context.SaveChangesAsync();

		return Ok("Ticket closed.");
	}
}