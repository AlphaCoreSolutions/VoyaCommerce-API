using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/features")]
public class NexusFeaturesController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public NexusFeaturesController(VoyaDbContext context) { _context = context; }

	// FEATURE 2: FEATURE FLAGS
	[HttpGet]
	public async Task<IActionResult> GetAllFlags()
	{
		return Ok(await _context.FeatureFlags.ToListAsync());
	}

	[HttpPost("toggle/{key}")]
	public async Task<IActionResult> ToggleFlag(string key, [FromBody] bool isEnabled)
	{
		var flag = await _context.FeatureFlags.FirstOrDefaultAsync(f => f.Key == key);
		if (flag == null)
		{
			flag = new FeatureFlag { Key = key, IsEnabled = isEnabled };
			_context.FeatureFlags.Add(flag);
		}
		else
		{
			flag.IsEnabled = isEnabled;
		}

		await _context.SaveChangesAsync();
		return Ok(new { Message = $"Feature {key} is now {(isEnabled ? "ON" : "OFF")}" });
	}

	// FEATURE 9: GLOBAL KILL SWITCH
	[HttpPost("maintenance-mode")]
	public async Task<IActionResult> SetMaintenanceMode([FromBody] bool enabled)
	{
		// Re-use logic for special key
		return await ToggleFlag("GlobalMaintenanceMode", enabled);
	}
}