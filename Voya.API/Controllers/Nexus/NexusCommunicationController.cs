using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Voya.API.Attributes;
using Voya.API.Hubs;
using Voya.Core.Constants;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/communication")]
public class NexusCommunicationController : ControllerBase
{
	private readonly IHubContext<LiveHub> _hubContext;
	private readonly VoyaDbContext _context;

	public NexusCommunicationController(IHubContext<LiveHub> hubContext, VoyaDbContext context)
	{
		_hubContext = hubContext;
		_context = context;
	}

	// -----------------------------
	// FEATURE 1: PUSH NOTIFICATION BLAST
	// POST /api/v1/nexus/communication/blast
	// -----------------------------
	[HttpPost("blast")]
	[RequirePermission(Permissions.MarketingManage)]
	public async Task<IActionResult> SendGlobalNotification([FromBody] NotificationBlastDto request)
	{
		if (request == null) return BadRequest("Body is required.");

		var title = (request.Title ?? "").Trim();
		var message = (request.Message ?? "").Trim();

		if (title.Length < 2) return BadRequest("Title is required.");
		if (message.Length < 2) return BadRequest("Message is required.");

		if (title.Length > 120) return BadRequest("Title is too long (max 120).");
		if (message.Length > 2000) return BadRequest("Message is too long (max 2000).");

		await _hubContext.Clients.All.SendAsync("ReceiveGlobalNotification", new
		{
			Title = title,
			Message = message,
			Type = "SystemAlert",
			Timestamp = DateTime.UtcNow
		});

		return Ok(new { Message = "Notification sent.", Title = title });
	}

	// -----------------------------
	// FEATURE 2: GLOBAL SMS GATEWAY (Placeholder)
	// POST /api/v1/nexus/communication/sms/send
	// -----------------------------
	[HttpPost("sms/send")]
	[RequirePermission(Permissions.SystemConfig)]
	public async Task<IActionResult> SendSms([FromBody] SmsRequestDto request)
	{
		if (request == null) return BadRequest("Body is required.");

		var phone = (request.PhoneNumber ?? "").Trim();
		var message = (request.Message ?? "").Trim();

		if (phone.Length < 6) return BadRequest("PhoneNumber is required.");
		if (message.Length < 1) return BadRequest("Message is required.");
		if (message.Length > 1600) return BadRequest("Message is too long.");

		phone = NormalizePhone(phone);
		if (string.IsNullOrWhiteSpace(phone))
			return BadRequest("Invalid PhoneNumber format.");

		// TODO:
		// 1) Validate credits
		// 2) Call provider
		// 3) Log to DB

		await Task.CompletedTask;

		return Ok(new
		{
			Message = "SMS queued (mock).",
			PhoneNumber = phone
		});
	}

	// -----------------------------
	// FEATURE 3: ANNOUNCEMENTS
	// POST /api/v1/nexus/communication/announcements
	// -----------------------------
	public class CreateAnnouncementDto
	{
		public string Title { get; set; } = "";
		public string Message { get; set; } = "";
		public AppType TargetApp { get; set; } = AppType.All;
		public string ColorHex { get; set; } = "#FF0000";
		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }
		public bool IsActive { get; set; } = true;
	}

	[HttpPost("announcements")]
	[RequirePermission(Permissions.ContentManage)]
	public async Task<IActionResult> CreateAnnouncement([FromBody] CreateAnnouncementDto dto)
	{
		if (dto == null) return BadRequest("Body is required.");

		var title = (dto.Title ?? "").Trim();
		var msg = (dto.Message ?? "").Trim();

		if (title.Length < 2) return BadRequest("Title is required.");
		if (msg.Length < 2) return BadRequest("Message is required.");
		if (title.Length > 120) return BadRequest("Title too long (max 120).");
		if (msg.Length > 4000) return BadRequest("Message too long (max 4000).");

		var color = (dto.ColorHex ?? "").Trim();
		if (!IsValidHexColor(color))
			return BadRequest("ColorHex must be a valid hex color like #FF0000.");

		// Normalize dates to UTC if they come as Unspecified
		var start = dto.StartDate.Kind == DateTimeKind.Unspecified
			? DateTime.SpecifyKind(dto.StartDate, DateTimeKind.Utc)
			: dto.StartDate.ToUniversalTime();

		var end = dto.EndDate.Kind == DateTimeKind.Unspecified
			? DateTime.SpecifyKind(dto.EndDate, DateTimeKind.Utc)
			: dto.EndDate.ToUniversalTime();

		if (end <= start)
			return BadRequest("EndDate must be after StartDate.");

		var ann = new Announcement
		{
			Title = title,
			Message = msg,
			TargetApp = dto.TargetApp,
			ColorHex = color,
			StartDate = start,
			EndDate = end,
			IsActive = dto.IsActive
		};

		_context.Announcements.Add(ann);
		await _context.SaveChangesAsync();

		var now = DateTime.UtcNow;

		// Only broadcast if active and currently in range
		if (ann.IsActive && ann.StartDate <= now && ann.EndDate > now)
		{
			await _hubContext.Clients.All.SendAsync("ReceiveGlobalBanner", new
			{
				ann.Id,
				ann.Title,
				ann.Message,
				TargetApp = ann.TargetApp.ToString(),
				ann.ColorHex,
				ann.StartDate,
				ann.EndDate,
				ann.IsActive
			});
		}

		return Ok(new
		{
			Message = "Announcement created.",
			ann.Id,
			ann.Title,
			ann.IsActive,
			TargetApp = ann.TargetApp.ToString(),
			ann.ColorHex,
			ann.StartDate,
			ann.EndDate
		});
	}

	// -----------------------------
	// Helpers
	// -----------------------------
	private static string NormalizePhone(string input)
	{
		var cleaned = Regex.Replace(input, @"[^\d\+]", "").Trim();

		if (cleaned.Count(c => c == '+') > 1) return "";
		if (cleaned.Contains('+') && !cleaned.StartsWith("+")) return "";

		var digits = cleaned.Replace("+", "");
		if (digits.Length < 6 || digits.Length > 16) return "";

		return cleaned;
	}

	private static bool IsValidHexColor(string value)
	{
		// Supports #RGB or #RRGGBB
		return Regex.IsMatch(value, "^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$");
	}
}

// DTOs
public class SmsRequestDto
{
	public string PhoneNumber { get; set; } = "";
	public string Message { get; set; } = "";
}

public class NotificationBlastDto
{
	public string Title { get; set; } = "";
	public string Message { get; set; } = "";
}
