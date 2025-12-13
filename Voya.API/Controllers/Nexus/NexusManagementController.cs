using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/management")]
public class NexusManagementController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public NexusManagementController(VoyaDbContext context) { _context = context; }

	// FEATURE 3: GLOBAL COUPONS
	[HttpPost("coupons/global")]
	public async Task<IActionResult> CreateGlobalCoupon([FromBody] GlobalCoupon coupon)
	{
		_context.GlobalCoupons.Add(coupon);
		await _context.SaveChangesAsync();
		return Ok("Global coupon created.");
	}

	// FEATURE 8: APP VERSION CONTROL
	[HttpGet("app-version")]
	[AllowAnonymous] // Public endpoint for apps to check
	public async Task<IActionResult> CheckVersion([FromQuery] string platform, [FromQuery] string currentVersion)
	{
		var config = await _context.AppVersionConfigs.FirstOrDefaultAsync(c => c.Platform == platform);
		if (config == null) return Ok(new { UpdateNeeded = false });

		// Logic: Compare versions (SemVer)
		// Simplified check:
		bool updateNeeded = config.MinVersion != currentVersion;

		return Ok(new
		{
			UpdateNeeded = updateNeeded,
			IsForceUpdate = config.ForceUpdate,
			Message = config.UpdateMessage,
			StoreUrl = "https://apps.apple.com/..."
		});
	}

	// FEATURE 7: DYNAMIC ONBOARDING FLOW
	[HttpGet("onboarding-config")]
	public IActionResult GetOnboardingConfig()
	{
		var steps = new List<OnboardingStepDto>
		{
			new OnboardingStepDto { Step = 1, Screen = "Welcome", Image = "url_1", IsSkippable = false },
			new OnboardingStepDto { Step = 2, Screen = "Interests", Image = null, IsSkippable = false },
			new OnboardingStepDto { Step = 3, Screen = "EnableNotifications", Image = null, IsSkippable = true }
		};
		return Ok(steps);
	}
}

public class OnboardingStepDto
{
	public int Step { get; set; }
	public string Screen { get; set; } = string.Empty;
	public string? Image { get; set; }
	public bool IsSkippable { get; set; }
}