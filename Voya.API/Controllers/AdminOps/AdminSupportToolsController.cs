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
[Route("api/v1/admin/support-tools")]
public class AdminSupportToolsController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public AdminSupportToolsController(VoyaDbContext context) { _context = context; }

	// FEATURE 1: BULK TICKET OPERATIONS
	[HttpPost("tickets/bulk")]
	[RequirePermission(Permissions.UsersManage)]
	public async Task<IActionResult> BulkTicketAction([FromBody] BulkActionDto request)
	{
		var tickets = await _context.Tickets
			.Where(t => request.TicketIds.Contains(t.Id))
			.ToListAsync();

		foreach (var t in tickets)
		{
			if (request.Action == "Close") t.Status = TicketStatus.Closed;
			if (request.Action == "MarkUrgent") t.Priority = TicketPriority.Urgent;
			// Add reply if provided
			if (!string.IsNullOrEmpty(request.ReplyMessage))
			{
				t.AdminResponse = request.ReplyMessage;
				t.Status = TicketStatus.Resolved;
				t.ResolvedAt = DateTime.UtcNow;
			}
		}

		await _context.SaveChangesAsync();
		return Ok($"Processed {tickets.Count} tickets.");
	}

	// FEATURE 2: CANNED RESPONSES (Macros)
	[HttpGet("macros")]
	public async Task<IActionResult> GetMacros() => Ok(await _context.CannedResponses.ToListAsync());

	[HttpPost("macros")]
	[RequirePermission(Permissions.ContentManage)]
	public async Task<IActionResult> CreateMacro([FromBody] CannedResponse macro)
	{
		_context.CannedResponses.Add(macro);
		await _context.SaveChangesAsync();
		return Ok(macro);
	}

	// FEATURE 5: SLA BREACH DETECTION
	[HttpGet("tickets/sla-breach")]
	public async Task<IActionResult> GetSlaBreaches()
	{
		var now = DateTime.UtcNow;
		// Logic: Urgent tickets > 2 hours old, High > 24 hours old
		var urgentCutoff = now.AddHours(-2);
		var highCutoff = now.AddHours(-24);

		var breaches = await _context.Tickets
			.Where(t => t.Status == TicketStatus.Open &&
					   ((t.Priority == TicketPriority.Urgent && t.CreatedAt < urgentCutoff) ||
						(t.Priority == TicketPriority.High && t.CreatedAt < highCutoff)))
			.Select(t => new { t.Id, t.Subject, t.Priority, HoursOpen = (now - t.CreatedAt).TotalHours })
			.ToListAsync();

		return Ok(breaches);
	}
}

public class BulkActionDto
{
	public List<Guid> TicketIds { get; set; } = new();
	public string Action { get; set; } = ""; // "Close", "MarkUrgent"
	public string? ReplyMessage { get; set; }
}