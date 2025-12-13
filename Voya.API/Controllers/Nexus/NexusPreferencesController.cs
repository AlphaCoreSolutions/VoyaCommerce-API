using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Voya.API.Controllers.Nexus;

[ApiController]
[Route("api/v1/nexus/preferences")]
public class NexusPreferencesController : ControllerBase
{
	// FEATURE 9: DARK MODE (Time-Automated + Manual)
	// The App calls this on startup to decide which theme to load
	[HttpGet("theme")]
	public IActionResult GetThemePreference([FromQuery] string userId)
	{
		// 1. Check for manual override in DB (Mocked)
		bool manualOverride = false; // Check User Preferences table

		if (manualOverride) return Ok(new { Theme = "Dark" });

		// 2. Time-Based Automation (Auto Dark Mode after 7 PM)
		var hour = DateTime.Now.Hour; // Use User's local time if sent in headers, else Server time
		var isNight = hour >= 19 || hour <= 6;

		return Ok(new
		{
			Theme = isNight ? "Dark" : "Light",
			IsAuto = true
		});
	}
}