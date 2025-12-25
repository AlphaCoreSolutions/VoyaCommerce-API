using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.API.Attributes;
using Voya.Core.Constants;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/config")]
public class NexusConfigController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public NexusConfigController(VoyaDbContext context) { _context = context; }

	// GET /api/v1/nexus/config
	[HttpGet]
	[RequirePermission(Permissions.SettingsView)]
	public async Task<IActionResult> GetSettings([FromQuery] int take = 200)
	{
		if (take < 1) take = 200;
		if (take > 500) take = 500;

		// Security: read-only, no tracking
		var settings = await _context.GlobalSettings
			.AsNoTracking()
			.OrderBy(s => s.Key)
			.Take(take)
			.Select(s => new
			{
				s.Key,
				s.Value,
				s.UpdatedAt
			})
			.ToListAsync();

		return Ok(new
		{
			Take = take,
			Items = settings
		});
	}

	public class UpdateSettingDto
	{
		public string Value { get; set; } = string.Empty;
	}

	// PUT /api/v1/nexus/config/{key}
	[HttpPut("{key}")]
	[RequirePermission(Permissions.SettingsManage)]
	public async Task<IActionResult> UpdateSetting(string key, [FromBody] UpdateSettingDto request)
	{
		key = (key ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(key))
			return BadRequest("Key is required.");

		if (request == null)
			return BadRequest("Body is required.");

		// Prevent huge payloads (basic hardening)
		var value = (request.Value ?? "").Trim();
		if (value.Length > 4000)
			return BadRequest("Value is too long.");

		var setting = await _context.GlobalSettings.FirstOrDefaultAsync(s => s.Key.ToLower() == key);
		if (setting == null) return NotFound();

		setting.Value = value;
		setting.UpdatedAt = DateTime.UtcNow;

		await _context.SaveChangesAsync();

		return Ok(new
		{
			Message = "Setting updated.",
			Key = setting.Key,
			Value = setting.Value,
			UpdatedAt = setting.UpdatedAt
		});
	}
}
