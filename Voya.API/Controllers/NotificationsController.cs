using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.Application.DTOs;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/notifications")]
public class NotificationsController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public NotificationsController(VoyaDbContext context)
	{
		_context = context;
	}

	private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")!.Value);

	[HttpGet]
	public async Task<ActionResult<List<NotificationDto>>> GetNotifications()
	{
		var userId = GetUserId();
		var notifs = await _context.Notifications
			.Where(n => n.UserId == userId)
			.OrderByDescending(n => n.CreatedAt)
			.Select(n => new NotificationDto(n.Id, n.Title, n.Body, n.IsRead, n.CreatedAt.ToString("g")))
			.ToListAsync();

		return Ok(notifs);
	}

	[HttpPut("{id}/read")]
	public async Task<IActionResult> MarkAsRead(Guid id)
	{
		var userId = GetUserId();
		var notif = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
		if (notif != null)
		{
			notif.IsRead = true;
			await _context.SaveChangesAsync();
		}
		return Ok();
	}
}