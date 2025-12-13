using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/payroll")]
public class NexusPayrollController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public NexusPayrollController(VoyaDbContext context) { _context = context; }

	// FEATURE 6: PAYROLL CALCULATION
	[HttpPost("calculate")]
	public async Task<IActionResult> CalculatePayroll([FromQuery] int month, [FromQuery] int year)
	{
		// 1. Get all staff shifts for the month
		var shifts = await _context.StaffShifts
			.Where(s => s.ClockInTime.Month == month && s.ClockInTime.Year == year && s.ClockOutTime != null)
			.ToListAsync();

		var report = shifts
			.GroupBy(s => s.StaffUserId)
			.Select(g => new
			{
				UserId = g.Key,
				TotalHours = g.Sum(x => x.TotalHours),
				EstimatedPay = g.Sum(x => x.TotalHours) * 5.0m // Mock Hourly Rate $5
			})
			.ToList();

		// In a real app, save to PayrollRecords table here
		return Ok(report);
	}

	// FEATURE 9: PAYOUT AUTOMATION (Mockup Structure)
	[HttpPost("payout/vendor-batch")]
	public IActionResult RunVendorPayouts()
	{
		// Mocking the Service Call
		// _payoutService.ProcessBatch(DateTime.UtcNow);
		return Ok(new { Message = "Payout batch job queued.", EstimatedTransactions = 150 });
	}
}