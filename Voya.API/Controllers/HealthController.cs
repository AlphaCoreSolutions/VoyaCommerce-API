using Microsoft.AspNetCore.Mvc;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public HealthController(VoyaDbContext context) { _context = context; }

	// FEATURE 8: API HEALTH CHECK
	[HttpGet]
	public IActionResult Check()
	{
		bool dbConnected = _context.Database.CanConnect();
		return Ok(new
		{
			Status = "Online",
			DbConnection = dbConnected ? "Healthy" : "Failed",
			Timestamp = DateTime.UtcNow
		});
	}
}