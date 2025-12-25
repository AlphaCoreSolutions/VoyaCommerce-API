using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.API.Attributes;
using Voya.Core.Constants;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/intelligence")]
public class NexusIntelligenceController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public NexusIntelligenceController(VoyaDbContext context) { _context = context; }

	// ------------------------------------------------------------
	// AI CONTROL CENTER - OVERVIEW
	// GET /api/v1/nexus/intelligence/overview
	// ------------------------------------------------------------
	[HttpGet("overview")]
	[RequirePermission(Permissions.AiView)]
	public async Task<IActionResult> Overview()
	{
		var health = await GetHealthInternal();

		var flags = await _context.AiFeatureFlags
			.AsNoTracking()
			.OrderBy(f => f.Key)
			.Select(f => new
			{
				f.Id,
				f.Key,
				f.IsEnabled,
				f.ConfigJson,
				f.UpdatedAt
			})
			.ToListAsync();

		var jobs = await _context.AiJobRuns
			.AsNoTracking()
			.OrderByDescending(j => j.StartedAt)
			.Take(20)
			.Select(j => new
			{
				j.Id,
				j.JobKey,
				j.Status,
				j.StartedAt,
				j.FinishedAt,
				j.Summary
			})
			.ToListAsync();

		return Ok(new
		{
			Health = health,
			Flags = flags,
			RecentJobs = jobs
		});
	}

	// ------------------------------------------------------------
	// FEATURE FLAGS
	// GET /api/v1/nexus/intelligence/flags
	// POST /api/v1/nexus/intelligence/flags (upsert)
	// ------------------------------------------------------------
	[HttpGet("flags")]
	[RequirePermission(Permissions.AiView)]
	public async Task<IActionResult> GetFlags()
	{
		var flags = await _context.AiFeatureFlags
			.AsNoTracking()
			.OrderBy(f => f.Key)
			.Select(f => new
			{
				f.Id,
				f.Key,
				f.IsEnabled,
				f.ConfigJson,
				f.UpdatedAt
			})
			.ToListAsync();

		return Ok(flags);
	}

	public class UpsertAiFlagRequest
	{
		public string Key { get; set; } = "";
		public bool IsEnabled { get; set; }
		public string ConfigJson { get; set; } = "{}";
	}

	[HttpPost("flags")]
	[RequirePermission(Permissions.AiManage)]
	public async Task<IActionResult> UpsertFlag([FromBody] UpsertAiFlagRequest req)
	{
		var key = (req.Key ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(key))
			return BadRequest("Key is required.");

		var flag = await _context.AiFeatureFlags.FirstOrDefaultAsync(f => f.Key == key);

		if (flag == null)
		{
			flag = new AiFeatureFlag
			{
				Key = key,
				IsEnabled = req.IsEnabled,
				ConfigJson = string.IsNullOrWhiteSpace(req.ConfigJson) ? "{}" : req.ConfigJson,
				UpdatedAt = DateTime.UtcNow,
				UpdatedByUserId = TryGetUserId()
			};
			_context.AiFeatureFlags.Add(flag);
		}
		else
		{
			flag.IsEnabled = req.IsEnabled;
			flag.ConfigJson = string.IsNullOrWhiteSpace(req.ConfigJson) ? flag.ConfigJson : req.ConfigJson;
			flag.UpdatedAt = DateTime.UtcNow;
			flag.UpdatedByUserId = TryGetUserId();
		}

		await _context.SaveChangesAsync();

		return Ok(new
		{
			flag.Id,
			flag.Key,
			flag.IsEnabled,
			flag.UpdatedAt
		});
	}

	// ------------------------------------------------------------
	// JOB RUNS
	// GET /api/v1/nexus/intelligence/jobs
	// GET /api/v1/nexus/intelligence/jobs/{id}
	// POST /api/v1/nexus/intelligence/jobs/run  (manual run by jobKey)
	// ------------------------------------------------------------
	[HttpGet("jobs")]
	[RequirePermission(Permissions.AiView)]
	public async Task<IActionResult> GetJobs([FromQuery] int take = 50)
	{
		if (take < 1) take = 50;
		if (take > 200) take = 200;

		var jobs = await _context.AiJobRuns
			.AsNoTracking()
			.OrderByDescending(j => j.StartedAt)
			.Take(take)
			.Select(j => new
			{
				j.Id,
				j.JobKey,
				j.Status,
				j.StartedAt,
				j.FinishedAt,
				j.Summary
			})
			.ToListAsync();

		return Ok(jobs);
	}

	[HttpGet("jobs/{id:guid}")]
	[RequirePermission(Permissions.AiView)]
	public async Task<IActionResult> GetJob(Guid id)
	{
		var j = await _context.AiJobRuns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
		if (j == null) return NotFound();

		return Ok(new
		{
			j.Id,
			j.JobKey,
			j.Status,
			j.StartedAt,
			j.FinishedAt,
			j.Summary,
			j.Logs
		});
	}

	public class RunAiJobRequest
	{
		public string JobKey { get; set; } = "";
	}

	[HttpPost("jobs/run")]
	[RequirePermission(Permissions.AiManage)]
	public async Task<IActionResult> RunJob([FromBody] RunAiJobRequest req)
	{
		var jobKey = (req.JobKey ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(jobKey))
			return BadRequest("JobKey is required.");

		// Create a queued job run record.
		// Later you can wire this into Hangfire/Quartz/BackgroundService.
		var run = new AiJobRun
		{
			JobKey = jobKey,
			Status = "queued",
			StartedAt = DateTime.UtcNow,
			Summary = "Manual trigger from Nexus Intelligence",
			TriggeredByUserId = TryGetUserId()
		};

		_context.AiJobRuns.Add(run);
		await _context.SaveChangesAsync();

		return Ok(new { run.Id, run.JobKey, run.Status, run.StartedAt });
	}

	// ------------------------------------------------------------
	// 1) PREDICTIVE INVENTORY (Real Logic - current heuristic)
	// GET /api/v1/nexus/intelligence/inventory-predictions
	// ------------------------------------------------------------
	[HttpGet("inventory-predictions")]
	[RequirePermission(Permissions.AiView)]
	public async Task<IActionResult> GetInventoryPredictions()
	{
		// Simple Heuristic: Stock < 20
		var lowStockProducts = await _context.Products
			.AsNoTracking()
			.Where(p => p.StockQuantity < 20)
			.OrderBy(p => p.StockQuantity)
			.Select(p => new
			{
				p.Id,
				p.Name,
				p.StockQuantity
			})
			.Take(20)
			.ToListAsync();

		return Ok(new
		{
			GeneratedAtUtc = DateTime.UtcNow,
			AlertCount = lowStockProducts.Count,
			Items = lowStockProducts
		});
	}

	// ------------------------------------------------------------
	// 2) SYSTEM HEALTH (Spider Log)
	// GET /api/v1/nexus/intelligence/system-health
	// ------------------------------------------------------------
	[HttpGet("system-health")]
	[RequirePermission(Permissions.AiView)]
	public async Task<IActionResult> GetSystemHealth()
	{
		var health = await GetHealthInternal();
		return Ok(health);
	}

	// ------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------
	private async Task<object> GetHealthInternal()
	{
		var db = "unknown";
		var dbLatencyMs = -1;

		try
		{
			var sw = System.Diagnostics.Stopwatch.StartNew();
			await _context.Database.ExecuteSqlRawAsync("SELECT 1");
			sw.Stop();

			db = "ok";
			dbLatencyMs = (int)sw.ElapsedMilliseconds;
		}
		catch
		{
			db = "down";
		}

		return new
		{
			Status = db == "ok" ? "Nominal" : "Degraded",
			Api = "ok",
			Database = db,
			DbLatencyMs = dbLatencyMs,
			TimestampUtc = DateTime.UtcNow
		};
	}

	private Guid? TryGetUserId()
	{
		var raw = User.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == "id")?.Value;
		return Guid.TryParse(raw, out var id) ? id : null;
	}
}
