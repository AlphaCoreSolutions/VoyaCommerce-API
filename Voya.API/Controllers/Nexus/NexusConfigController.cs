using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/config")]
public class NexusConfigController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public NexusConfigController(VoyaDbContext context) { _context = context; }

	[HttpGet]
	public async Task<IActionResult> GetSettings()
	{
		return Ok(await _context.GlobalSettings.ToListAsync());
	}

	[HttpPut("{key}")]
	public async Task<IActionResult> UpdateSetting(string key, [FromBody] UpdateSettingDto request)
	{
		var setting = await _context.GlobalSettings.FirstOrDefaultAsync(s => s.Key == key);
		if (setting == null) return NotFound();

		setting.Value = request.Value;
		setting.UpdatedAt = DateTime.UtcNow;

		await _context.SaveChangesAsync();
		return Ok($"Setting {key} updated.");
	}
}

public class UpdateSettingDto { public string Value { get; set; } = string.Empty; }