using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Voya.API.Attributes;
using Voya.Core.Constants;
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

	// Allow keys like: "global_maintenance_mode", "inventory_predictions", "checkout_v2"
	private static readonly Regex KeyRegex = new(@"^[a-z0-9_\-\.]{2,64}$", RegexOptions.Compiled);

	private static string NormalizeKey(string key)
		=> (key ?? "").Trim().ToLowerInvariant();

	// FEATURE FLAGS
	// GET /api/v1/nexus/features
	[HttpGet]
	[RequirePermission(Permissions.SettingsView)]
	public async Task<IActionResult> GetAllFlags([FromQuery] int take = 200)
	{
		if (take < 1) take = 200;
		if (take > 500) take = 500;

		var flags = await _context.FeatureFlags
			.AsNoTracking()
			.OrderBy(f => f.Key)
			.Take(take)
			.Select(f => new
			{
				f.Id,
				f.Key,
				f.IsEnabled,
				f.UpdatedAt
			})
			.ToListAsync();

		return Ok(new
		{
			Take = take,
			Items = flags
		});
	}

	public class ToggleFlagDto
	{
		public bool IsEnabled { get; set; }
	}

	// POST /api/v1/nexus/features/toggle/{key}
	[HttpPost("toggle/{key}")]
	[RequirePermission(Permissions.SettingsManage)]
	public async Task<IActionResult> ToggleFlag(string key, [FromBody] ToggleFlagDto body)
	{
		var normalizedKey = NormalizeKey(key);

		if (string.IsNullOrWhiteSpace(normalizedKey))
			return BadRequest("Key is required.");

		if (!KeyRegex.IsMatch(normalizedKey))
			return BadRequest("Invalid key format. Use 2-64 chars: lowercase letters, numbers, _ - .");

		var flag = await _context.FeatureFlags.FirstOrDefaultAsync(f => f.Key.ToLower() == normalizedKey);

		if (flag == null)
		{
			flag = new FeatureFlag
			{
				Key = normalizedKey,
				IsEnabled = body.IsEnabled,
				UpdatedAt = DateTime.UtcNow
			};
			_context.FeatureFlags.Add(flag);
		}
		else
		{
			flag.IsEnabled = body.IsEnabled;
			flag.UpdatedAt = DateTime.UtcNow;
		}

		await _context.SaveChangesAsync();

		return Ok(new
		{
			Message = "Feature flag updated.",
			Key = flag.Key,
			IsEnabled = flag.IsEnabled,
			UpdatedAt = flag.UpdatedAt
		});
	}

	// GLOBAL KILL SWITCH
	// POST /api/v1/nexus/features/maintenance-mode
	[HttpPost("maintenance-mode")]
	[RequirePermission(Permissions.SettingsManage)]
	public async Task<IActionResult> SetMaintenanceMode([FromBody] ToggleFlagDto body)
	{
		// normalized special key (stable)
		return await ToggleFlag("global_maintenance_mode", body);
	}
}
