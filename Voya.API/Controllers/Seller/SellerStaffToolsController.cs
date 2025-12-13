using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

[Authorize]
[ApiController]
[Route("api/v1/seller/staff-tools")]
public class SellerStaffToolsController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public SellerStaffToolsController(VoyaDbContext context) { _context = context; }

	[HttpPost("clock-in")]
	public async Task<IActionResult> ClockIn(Guid storeId)
	{
		var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

		// Check if already clocked in
		var activeShift = await _context.StaffShifts
			.FirstOrDefaultAsync(s => s.StaffUserId == userId && s.ClockOutTime == null);

		if (activeShift != null) return BadRequest("You are already clocked in.");

		_context.StaffShifts.Add(new StaffShift
		{
			StoreId = storeId,
			StaffUserId = userId
		});
		await _context.SaveChangesAsync();
		return Ok("Clocked In.");
	}

	[HttpPost("clock-out")]
	public async Task<IActionResult> ClockOut()
	{
		var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

		var activeShift = await _context.StaffShifts
			.FirstOrDefaultAsync(s => s.StaffUserId == userId && s.ClockOutTime == null);

		if (activeShift == null) return BadRequest("No active shift found.");

		activeShift.ClockOutTime = DateTime.UtcNow;
		await _context.SaveChangesAsync();
		return Ok($"Clocked Out. Total hours: {activeShift.TotalHours:F2}");
	}
}