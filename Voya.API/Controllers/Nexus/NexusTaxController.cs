using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.API.Attributes;
using Voya.Core.Constants;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;
using Voya.Application.DTOs.Nexus;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/tax")]
public class NexusTaxController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public NexusTaxController(VoyaDbContext context)
	{
		_context = context;
	}

	// ---------------------
	// Rules
	// ---------------------
	[HttpGet("rules")]
	[RequirePermission(Permissions.TaxView)]
	public async Task<IActionResult> GetRules()
	{
		var rules = await _context.TaxRules
			.AsNoTracking()
			.OrderBy(r => r.Priority)
			.ThenBy(r => r.CountryCode)
			.ThenBy(r => r.Name)
			.Select(r => new NexusTaxRuleDto
			{
				Id = r.Id,
				Name = r.Name,
				CountryCode = r.CountryCode,
				Region = r.Region,
				City = r.City,
				CategoryId = r.CategoryId,
				StoreId = r.StoreId,
				RatePercent = r.RatePercent,
				IsActive = r.IsActive,
				Priority = r.Priority
			})
			.ToListAsync();

		return Ok(rules);
	}

	[HttpPost("rules")]
	[RequirePermission(Permissions.TaxManage)]
	public async Task<IActionResult> CreateRule([FromBody] NexusUpsertTaxRuleRequest req)
	{
		var rule = new TaxRule
		{
			Name = req.Name,
			CountryCode = req.CountryCode.ToUpper().Trim(),
			Region = string.IsNullOrWhiteSpace(req.Region) ? null : req.Region.Trim(),
			City = string.IsNullOrWhiteSpace(req.City) ? null : req.City.Trim(),
			CategoryId = req.CategoryId,
			StoreId = req.StoreId,
			RatePercent = req.RatePercent,
			IsActive = req.IsActive,
			Priority = req.Priority,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};

		_context.TaxRules.Add(rule);
		await _context.SaveChangesAsync();

		return Ok(new { rule.Id });
	}

	[HttpPut("rules/{id:guid}")]
	[RequirePermission(Permissions.TaxManage)]
	public async Task<IActionResult> UpdateRule(Guid id, [FromBody] NexusUpsertTaxRuleRequest req)
	{
		var rule = await _context.TaxRules.FirstOrDefaultAsync(x => x.Id == id);
		if (rule == null) return NotFound();

		rule.Name = req.Name;
		rule.CountryCode = req.CountryCode.ToUpper().Trim();
		rule.Region = string.IsNullOrWhiteSpace(req.Region) ? null : req.Region.Trim();
		rule.City = string.IsNullOrWhiteSpace(req.City) ? null : req.City.Trim();
		rule.CategoryId = req.CategoryId;
		rule.StoreId = req.StoreId;
		rule.RatePercent = req.RatePercent;
		rule.IsActive = req.IsActive;
		rule.Priority = req.Priority;
		rule.UpdatedAt = DateTime.UtcNow;

		await _context.SaveChangesAsync();
		return Ok(new { rule.Id });
	}

	// Soft disable instead of deleting
	[HttpDelete("rules/{id:guid}")]
	[RequirePermission(Permissions.TaxManage)]
	public async Task<IActionResult> DisableRule(Guid id)
	{
		var rule = await _context.TaxRules.FirstOrDefaultAsync(x => x.Id == id);
		if (rule == null) return NotFound();

		rule.IsActive = false;
		rule.UpdatedAt = DateTime.UtcNow;
		await _context.SaveChangesAsync();

		return Ok(new { rule.Id, rule.IsActive });
	}

	// ---------------------
	// Settings
	// ---------------------
	[HttpGet("settings")]
	[RequirePermission(Permissions.TaxView)]
	public async Task<IActionResult> GetSettings()
	{
		var settings = await _context.TaxSettings.AsNoTracking().FirstOrDefaultAsync();
		if (settings == null)
		{
			settings = new TaxSettings();
			_context.TaxSettings.Add(settings);
			await _context.SaveChangesAsync();
		}

		return Ok(new NexusTaxSettingsDto
		{
			TaxEnabled = settings.TaxEnabled,
			DefaultRatePercent = settings.DefaultRatePercent,
			PricesIncludeTax = settings.PricesIncludeTax
		});
	}

	[HttpPost("settings")]
	[RequirePermission(Permissions.TaxManage)]
	public async Task<IActionResult> UpdateSettings([FromBody] NexusUpdateTaxSettingsRequest req)
	{
		var settings = await _context.TaxSettings.FirstOrDefaultAsync();
		if (settings == null)
		{
			settings = new TaxSettings();
			_context.TaxSettings.Add(settings);
		}

		settings.TaxEnabled = req.TaxEnabled;
		settings.DefaultRatePercent = req.DefaultRatePercent;
		settings.PricesIncludeTax = req.PricesIncludeTax;
		settings.UpdatedAt = DateTime.UtcNow;

		await _context.SaveChangesAsync();
		return Ok();
	}

	// ---------------------
	// Preview
	// ---------------------
	[HttpPost("preview")]
	[RequirePermission(Permissions.TaxView)]
	public async Task<IActionResult> Preview([FromBody] NexusTaxPreviewRequest req)
	{
		var settings = await _context.TaxSettings.AsNoTracking().FirstOrDefaultAsync()
					  ?? new TaxSettings();

		if (!settings.TaxEnabled)
		{
			return Ok(new NexusTaxPreviewResponse
			{
				RatePercent = 0,
				TaxAmount = 0,
				TotalWithTax = req.SubTotal,
				AppliedRuleName = "Tax disabled"
			});
		}

		var normalizedCountry = req.CountryCode.ToUpper().Trim();
		var region = string.IsNullOrWhiteSpace(req.Region) ? null : req.Region.Trim();
		var city = string.IsNullOrWhiteSpace(req.City) ? null : req.City.Trim();

		// Find best matching active rule (lowest priority)
		var rule = await _context.TaxRules.AsNoTracking()
			.Where(r => r.IsActive && r.CountryCode == normalizedCountry)
			.Where(r => r.StoreId == null || r.StoreId == req.StoreId)
			.Where(r => r.CategoryId == null || r.CategoryId == req.CategoryId)
			.Where(r => r.Region == null || r.Region == region)
			.Where(r => r.City == null || r.City == city)
			.OrderBy(r => r.Priority)
			.FirstOrDefaultAsync();

		var rate = rule?.RatePercent ?? settings.DefaultRatePercent;

		var taxAmount = Math.Round(req.SubTotal * (rate / 100m), 2);
		var total = req.SubTotal + taxAmount;

		return Ok(new NexusTaxPreviewResponse
		{
			RatePercent = rate,
			TaxAmount = taxAmount,
			TotalWithTax = total,
			AppliedRuleName = rule?.Name ?? "Default rate"
		});
	}
}
