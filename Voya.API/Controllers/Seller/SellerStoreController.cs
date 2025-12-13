using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Seller;

[Authorize]
[ApiController]
[Route("api/v1/seller/store")]
public class SellerStoreController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public SellerStoreController(VoyaDbContext context)
	{
		_context = context;
	}

	private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

	// --- 1. STORE CUSTOMIZATION ---

	[HttpPut("settings")]
	public async Task<IActionResult> UpdateStoreSettings([FromBody] UpdateStoreSettingsDto request)
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);
		if (store == null) return Unauthorized();

		store.Name = request.Name;
		store.Description = request.Description;
		store.LogoUrl = request.LogoUrl;
		store.CoverImageUrl = request.CoverImageUrl;

		// Save these in a JSON field ideally, or specific columns if added
		// store.BrandColor = request.BrandColor; 

		await _context.SaveChangesAsync();
		return Ok("Store settings updated.");
	}

	// --- 2. STAFF MANAGEMENT ---

	[HttpGet("staff")]
	public async Task<IActionResult> GetStaff()
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);

		var staff = await _context.StoreStaff
			.Include(s => s.User)
			.Where(s => s.StoreId == store!.Id)
			.Select(s => new { s.Id, s.User.FullName, s.User.Email, s.Role })
			.ToListAsync();

		return Ok(staff);
	}

	[HttpPost("staff")]
	public async Task<IActionResult> AddStaff(string email, StoreRole role)
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);

		var userToAdd = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
		if (userToAdd == null) return NotFound("User must register on Voya first.");

		if (await _context.StoreStaff.AnyAsync(s => s.StoreId == store!.Id && s.UserId == userToAdd.Id))
			return BadRequest("User is already staff.");

		var newStaff = new StoreStaff
		{
			StoreId = store!.Id,
			UserId = userToAdd.Id,
			Role = role
		};

		_context.StoreStaff.Add(newStaff);
		await _context.SaveChangesAsync();
		return Ok("Staff added.");
	}

	// --- 3. LIVE STREAM (COMING SOON STRUCTURE) ---

	[HttpPost("live/schedule")]
	public async Task<IActionResult> ScheduleLiveEvent([FromBody] LiveEvent request)
	{
		// 1. Validate structure is correct
		if (string.IsNullOrEmpty(request.Title) || request.ScheduledAt < DateTime.UtcNow)
			return BadRequest("Invalid event details.");

		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);
		if (store == null) return Unauthorized();

		// 2. Save to DB (So it works logically)
		request.StoreId = store.Id;
		request.Status = LiveEventStatus.Scheduled;

		_context.LiveEvents.Add(request);
		await _context.SaveChangesAsync();

		// 3. Return "Success" but UI handles "Coming Soon" display
		return Ok(new
		{
			Message = "Event scheduled successfully.",
			Note = "Live streaming is currently in beta. You will be notified when your slot is active.",
			EventId = request.Id
		});
	}

	[HttpGet("live")]
	public async Task<IActionResult> GetMyLiveEvents()
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);

		var events = await _context.LiveEvents
			.Where(e => e.StoreId == store!.Id)
			.OrderByDescending(e => e.ScheduledAt)
			.ToListAsync();

		return Ok(events);
	}

	// FEATURE 8: VACATION & AUTO-CLOSE CONFIG

	[HttpPost("vacation")]
	public async Task<IActionResult> ToggleVacation([FromBody] bool isOn)
	{
		var store = await GetMyStore();
		store.IsOnVacation = isOn;
		store.IsCurrentlyOpen = !isOn; // If on vacation, force close
		await _context.SaveChangesAsync();
		return Ok($"Vacation mode is now {(isOn ? "ON" : "OFF")}");
	}

	[HttpPost("auto-close/request")]
	public async Task<IActionResult> RequestAutoCloseFeature([FromBody] bool enable)
	{
		var store = await GetMyStore();
		// Just sets the preference, Admin/Job decides execution
		store.AutoCloseEnabled = enable;
		await _context.SaveChangesAsync();
		return Ok("Preference saved. Waiting for next schedule cycle.");
	}

	// --- HELPER METHOD ---
	private async Task<Store> GetMyStore()
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);

		if (store == null)
		{
			// In a real scenario, you might handle this differently, 
			// but since [Authorize] is on, this implies a data inconsistency 
			// or a user who hasn't registered a store yet.
			throw new Exception("Store not found for this user.");
		}

		return store;
	}
}

public class UpdateStoreSettingsDto
{
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public string? LogoUrl { get; set; }
	public string? CoverImageUrl { get; set; }
	public string? BrandColor { get; set; }
}