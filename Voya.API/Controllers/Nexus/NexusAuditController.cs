using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.API.Attributes;
using Voya.Core.Constants;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/audit")]
public class NexusAuditController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public NexusAuditController(VoyaDbContext context) { _context = context; }

	// GET: api/v1/nexus/audit?limit=20
	[HttpGet]
	[RequirePermission(Permissions.SystemConfig)] // High-level permission
	public async Task<IActionResult> GetAuditLogs([FromQuery] int limit = 50)
	{
		var logs = await _context.SystemAuditLogs
			// .Include(l => l.AdminUser) // If you have a navigation property to User
			.OrderByDescending(l => l.Timestamp)
			.Take(limit)
			.Select(l => new
			{
				l.Id,
				Action = l.Action,
				// AdminName = l.AdminUser.FullName, // Uncomment if relation exists
				AdminName = "Admin " + l.AdminUserId.ToString().Substring(0, 4), // Mock Name if no relation
				EntityId = l.EntityId,
				Details = $"Changed {l.OldValue ?? "None"} to {l.NewValue ?? "None"}",
				Hash = l.Id.GetHashCode().ToString("X"), // Mock "Hash" for visualization
				Timestamp = l.Timestamp
			})
			.ToListAsync();

		return Ok(logs);
	}
}