using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Voya.Core.Entities;
using Voya.Core.Enums;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Seller;

[Authorize]
[ApiController]
[Route("api/v1/seller/onboarding")]
public class StoreOnboardingController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public StoreOnboardingController(VoyaDbContext context)
	{
		_context = context;
	}

	private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

	// 1. CHECK STATUS (For the "Status Bar" on Frontend)
	[HttpGet("status")]
	public async Task<IActionResult> GetMyStoreStatus()
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);

		if (store == null)
			return Ok(new { HasStore = false });

		return Ok(new
		{
			HasStore = true,
			StoreId = store.Id,
			Status = store.Status.ToString(),
			store.Name,
			store.AdminNotes // Show feedback if "ActionRequired"
		});
	}

	// 2. REGISTER (Submit Application)
	[HttpPost("register")]
	public async Task<IActionResult> RegisterStore([FromBody] RegisterStoreDto request)
	{
		var userId = GetUserId();

		// Ensure user doesn't already have a store
		if (await _context.Stores.AnyAsync(s => s.OwnerId == userId))
			return BadRequest("You already have a store application.");

		var store = new Store
		{
			OwnerId = userId,
			Name = request.Name,
			Description = request.Description,
			BusinessEmail = request.BusinessEmail,
			PhoneNumber = request.PhoneNumber,
			TaxNumber = request.TaxNumber,
			Address = request.Address,
			City = request.City,
			LogoUrl = request.LogoUrl,
			Status = StoreStatus.PendingReview // Goes straight to admin queue
		};

		_context.Stores.Add(store);
		await _context.SaveChangesAsync();

		return Ok(new { Message = "Application submitted successfully", StoreId = store.Id });
	}

	// 3. UPDATE INFO (If Admin requested changes)
	[HttpPut("update")]
	public async Task<IActionResult> UpdateStoreInfo([FromBody] RegisterStoreDto request)
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);

		if (store == null) return NotFound();

		// Only allow edits if not yet active (or if action required)
		if (store.Status == StoreStatus.Active)
			return BadRequest("Store is already active. Contact support to change legal details.");

		store.Name = request.Name;
		store.Description = request.Description;
		store.BusinessEmail = request.BusinessEmail;
		store.PhoneNumber = request.PhoneNumber;
		store.TaxNumber = request.TaxNumber;
		store.Address = request.Address;
		store.City = request.City;

		// If it was "ActionRequired", move it back to "PendingReview" automatically
		if (store.Status == StoreStatus.ActionRequired)
		{
			store.Status = StoreStatus.PendingReview;
		}

		await _context.SaveChangesAsync();
		return Ok("Store info updated.");
	}
}

// DTO
public class RegisterStoreDto
{
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public string BusinessEmail { get; set; } = string.Empty;
	public string PhoneNumber { get; set; } = string.Empty;
	public string TaxNumber { get; set; } = string.Empty;
	public string Address { get; set; } = string.Empty;
	public string City { get; set; } = string.Empty;
	public string? LogoUrl { get; set; }
}