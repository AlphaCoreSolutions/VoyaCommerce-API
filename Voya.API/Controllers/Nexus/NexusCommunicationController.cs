using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Voya.API.Hubs;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/communication")]
public class NexusCommunicationController : ControllerBase
{
	private readonly IHubContext<LiveHub> _hubContext;
	private readonly VoyaDbContext _context; // <--- FIX 1: Add Field

	// <--- FIX 2: Inject Context in Constructor
	public NexusCommunicationController(IHubContext<LiveHub> hubContext, VoyaDbContext context)
	{
		_hubContext = hubContext;
		_context = context;
	}

	// FEATURE 1: PUSH NOTIFICATION BLAST
	[HttpPost("blast")]
	public async Task<IActionResult> SendGlobalNotification([FromBody] NotificationBlastDto request)
	{
		await _hubContext.Clients.All.SendAsync("ReceiveGlobalNotification", new
		{
			Title = request.Title,
			Message = request.Message,
			Type = "SystemAlert",
			Timestamp = DateTime.UtcNow
		});

		return Ok("Notification sent to all connected users.");
	}

	// FEATURE 2: GLOBAL SMS GATEWAY (Placeholder)
	[HttpPost("sms/send")]
	public IActionResult SendSms([FromBody] SmsRequestDto request)
	{
		// 1. Validate Admin Credits
		// 2. Call Twilio/Unifonic API
		// 3. Log to DB
		return Ok($"SMS sent to {request.PhoneNumber} via Twilio (Mock).");
	}

	// FEATURE 3: ANNOUNCEMENTS
	[HttpPost("announcements")]
	public async Task<IActionResult> CreateAnnouncement([FromBody] Announcement ann)
	{
		_context.Announcements.Add(ann); // Now works because _context exists
		await _context.SaveChangesAsync();

		// Trigger SignalR blast immediately if active
		if (ann.IsActive && ann.StartDate <= DateTime.UtcNow)
		{
			await _hubContext.Clients.All.SendAsync("ReceiveGlobalBanner", ann);
		}
		return Ok("Announcement created.");
	}
}

// DTOs
public class SmsRequestDto { public string PhoneNumber { get; set; } = ""; public string Message { get; set; } = ""; }
public class NotificationBlastDto { public string Title { get; set; } = ""; public string Message { get; set; } = ""; }