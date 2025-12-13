using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Common;

[ApiController]
[Route("api/v1/engagement")]
public class EngagementController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public EngagementController(VoyaDbContext context) { _context = context; }

	// FEATURE 1: SUBSCRIBE TO STOCK
	[HttpPost("notify-stock")]
	public async Task<IActionResult> SubscribeToStock([FromBody] StockSubscription sub)
	{
		_context.StockSubscriptions.Add(sub);
		await _context.SaveChangesAsync();
		return Ok("You will be notified when stock returns.");
	}

	// FEATURE 3: DYNAMIC HOMEPAGE (Called by Mobile App on launch)
	[HttpGet("homepage-layout")]
	public async Task<IActionResult> GetHomepageLayout()
	{
		var widgets = await _context.HomepageWidgets
			.Where(w => w.IsActive)
			.OrderBy(w => w.SortOrder)
			.ToListAsync();
		return Ok(widgets);
	}
}