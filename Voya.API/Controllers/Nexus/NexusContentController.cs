using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/content")]
public class NexusContentController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public NexusContentController(VoyaDbContext context) { _context = context; }

	// FEATURE 6: LOCALIZATION MANAGER

	[HttpGet("translations")]
	public async Task<IActionResult> GetAllTranslations()
	{
		return Ok(await _context.LocalizationResources.ToListAsync());
	}

	[HttpPut("translation")]
	public async Task<IActionResult> UpdateTranslation([FromBody] LocalizationResource req)
	{
		var existing = await _context.LocalizationResources
			.FirstOrDefaultAsync(r => r.Key == req.Key && r.LanguageCode == req.LanguageCode);

		if (existing == null)
		{
			_context.LocalizationResources.Add(req);
		}
		else
		{
			existing.Value = req.Value;
		}

		await _context.SaveChangesAsync();
		return Ok("Translation saved.");
	}

	// FEATURE 10: WHITE-LABEL CONFIG
	[HttpPost("branding")]
	public IActionResult UpdateBranding([FromBody] BrandingDto req)
	{
		// Save to GlobalSettings table (conceptual)
		return Ok($"App Name updated to {req.AppName}");
	}
}

public class BrandingDto { public string AppName { get; set; } = ""; }