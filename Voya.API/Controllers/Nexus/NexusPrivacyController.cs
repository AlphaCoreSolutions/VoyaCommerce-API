using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/privacy")]
public class NexusPrivacyController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public NexusPrivacyController(VoyaDbContext context) { _context = context; }

	// FEATURE 3: GDPR DATA EXPORT
	[HttpGet("export/{userId}")]
	public async Task<IActionResult> ExportUserData(Guid userId)
	{
		var user = await _context.Users.FindAsync(userId);
		if (user == null) return NotFound();

		var orders = await _context.Orders.Where(o => o.UserId == userId).ToListAsync();
		var reviews = await _context.Reviews.Where(r => r.UserId == userId).ToListAsync();
		var addresses = await _context.Addresses.Where(a => a.UserId == userId).ToListAsync();

		var exportPackage = new
		{
			Profile = new { user.FullName, user.Email, user.CreatedAt },
			Orders = orders,
			Reviews = reviews,
			Addresses = addresses,
			GeneratedAt = DateTime.UtcNow
		};

		// In production: Return a downloadable JSON file or ZIP
		return Ok(exportPackage);
	}
}