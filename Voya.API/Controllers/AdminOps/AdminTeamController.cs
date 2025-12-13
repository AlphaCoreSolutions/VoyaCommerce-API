using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.AdminOps;

[Authorize]
[ApiController]
[Route("api/v1/admin/team")]
public class AdminTeamController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public AdminTeamController(VoyaDbContext context) { _context = context; }

	// FEATURE 8: STAFF PERFORMANCE DASHBOARD
	[HttpGet("performance")]
	public async Task<IActionResult> GetPerformanceStats()
	{
		// Calculate based on Ticket Resolutions in last 30 days
		var monthAgo = DateTime.UtcNow.AddDays(-30);

		// This query assumes we track 'ResolvedByAdminId' in tickets (need to add field or infer)
		// For now, we mock the logic structure based on SystemAuditLogs if available, 
		// or just return placeholder structure.

		var stats = new[]
		{
			new { Admin = "Sarah", TicketsResolved = 45, AvgResponseTime = "2.5h" },
			new { Admin = "Mike", TicketsResolved = 32, AvgResponseTime = "4.1h" }
		};

		return Ok(stats);
	}
}