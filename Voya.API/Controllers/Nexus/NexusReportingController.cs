using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.API.Attributes; // Required for [RequirePermission]
using Voya.Core.Constants; // Required for Permissions
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/reporting")] // Frontend must match this route
public class NexusReportingController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public NexusReportingController(VoyaDbContext context) { _context = context; }

	// --- 1. REPORT GENERATION (For Master Screen) ---

	// GET: api/v1/nexus/reporting/recent
	[HttpGet("recent")]
	[RequirePermission(Permissions.SystemConfig)]
	public async Task<IActionResult> GetRecentReports()
	{
		var reports = await _context.SystemReports
			.OrderByDescending(r => r.GeneratedAt)
			.Take(10)
			.ToListAsync();
		return Ok(reports);
	}

	// POST: api/v1/nexus/reporting/generate
	[HttpPost("generate")]
	[RequirePermission(Permissions.SystemConfig)]
	public async Task<IActionResult> GenerateReport([FromBody] ReportRequestDto request)
	{
		// Mock PDF Processing Delay (In real app: Send to Azure Function/Background Job)
		await Task.Delay(1500);

		var report = new SystemReport
		{
			Id = Guid.NewGuid(),
			Type = request.Type,
			// Generate a filename: "Financial_PL_20231027.pdf"
			Name = $"{request.Type.Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMdd}.pdf",
			GeneratedBy = User.Identity?.Name ?? "Super Admin",
			GeneratedAt = DateTime.UtcNow,
			DownloadUrl = "https://voya-storage.s3.amazonaws.com/reports/mock_report.pdf"
		};

		_context.SystemReports.Add(report);
		await _context.SaveChangesAsync();

		return Ok(report);
	}

	// --- 2. SCHEDULED REPORTS (Your Existing Feature) ---

	// POST: api/v1/nexus/reporting/schedule
	[HttpPost("schedule")]
	[RequirePermission(Permissions.SystemConfig)]
	public async Task<IActionResult> CreateSchedule([FromBody] ReportSchedule schedule)
	{
		if (schedule.Id == Guid.Empty) schedule.Id = Guid.NewGuid();

		_context.ReportSchedules.Add(schedule);
		await _context.SaveChangesAsync();
		return Ok("Report schedule saved.");
	}
}

// --- DTOs ---

public class ReportRequestDto
{
	public string Type { get; set; } = string.Empty;
	public DateTime StartDate { get; set; }
	public DateTime EndDate { get; set; }
}