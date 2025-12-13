using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Voya.Application.DTOs;
using Voya.Core.Entities;
using Voya.Core.Enums;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Seller;

[Authorize]
[ApiController]
[Route("api/v1/seller/marketing")]
public class SellerMarketingController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public SellerMarketingController(VoyaDbContext context)
	{
		_context = context;
	}

	private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

	// --- FEATURE 1: WISHLIST NUDGE (Corrected) ---

	// GET: See products sitting in wishlists > 3 days
	[HttpGet("leads")]
	public async Task<IActionResult> GetWishlistLeads()
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);
		if (store == null) return Unauthorized();

		var cutoffDate = DateTime.UtcNow.AddDays(-3);

		// FIX: Use WishlistItems and AddedAt
		var leads = await _context.WishlistItems
			.Include(w => w.Product)
			.Where(w => w.Product.StoreId == store.Id && w.AddedAt < cutoffDate)
			.GroupBy(w => new { w.ProductId, w.Product.Name })
			.Select(g => new
			{
				ProductId = g.Key.ProductId,
				ProductName = g.Key.Name,
				PotentialCustomersCount = g.Count(),
				OldestAddition = g.Min(x => x.AddedAt)
			})
			.ToListAsync();

		return Ok(leads);
	}

	// POST: Send "Nudge" Offer to all users who wishlisted this product
	[HttpPost("nudge")]
	public async Task<IActionResult> SendNudge([FromBody] SendTargetedOfferDto request)
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);
		if (store == null) return Unauthorized();

		var cutoffDate = DateTime.UtcNow.AddDays(-3);

		// FIX: Use WishlistItems and AddedAt
		var targets = await _context.WishlistItems
			.Include(w => w.Product)
			.Where(w => w.ProductId == request.ProductId && w.AddedAt < cutoffDate)
			.ToListAsync();

		if (!targets.Any()) return BadRequest("No eligible users found.");

		int count = 0;
		foreach (var target in targets)
		{
			// Create Unique Voucher Code
			var code = $"OFFER-{Guid.NewGuid().ToString().Substring(0, 5).ToUpper()}";

			var voucher = new Voucher
			{
				Code = code,
				Description = $"Special {request.DiscountPercentage}% off {target.Product.Name}",
				Type = DiscountType.Percentage,
				Value = request.DiscountPercentage,
				StartDate = DateTime.UtcNow,
				EndDate = DateTime.UtcNow.AddHours(24), // Valid for 24 hours
				MaxUsesPerUser = 1,
				StoreId = store.Id,
				IsActive = true
			};
			_context.Vouchers.Add(voucher);

			// Notify User
			var notif = new Notification
			{
				UserId = target.UserId,
				Title = "Private Offer! 🎁",
				Body = $"Get {request.DiscountPercentage}% off {target.Product.Name} if you buy in 24h! Code: {code}",
				Type = "Voucher",
				RelatedEntityId = code,
				CreatedAt = DateTime.UtcNow
			};
			_context.Notifications.Add(notif);
			count++;
		}

		await _context.SaveChangesAsync();
		return Ok(new { Message = $"Sent offers to {count} customers!" });
	}

	// --- FEATURE 2: GENERAL VOUCHERS (Same as before) ---
	[HttpPost("vouchers")]
	public async Task<IActionResult> CreateVoucher([FromBody] CreateVoucherDto request)
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);

		if (await _context.Vouchers.AnyAsync(v => v.Code == request.Code))
			return BadRequest("Voucher code already exists.");

		var voucher = new Voucher
		{
			Code = request.Code.ToUpper(),
			Description = request.Description,
			Type = request.Type,
			Value = request.Value,
			StartDate = request.StartDate,
			EndDate = request.EndDate,
			MaxUsesPerUser = request.MaxUsesPerUser,
			StoreId = store!.Id,
			IsActive = true
		};

		_context.Vouchers.Add(voucher);
		await _context.SaveChangesAsync();
		return Ok(new { Message = "Voucher created" });
	}

	// GET: api/v1/seller/marketing/vouchers
	[HttpGet("vouchers")]
	public async Task<ActionResult<List<VoucherDto>>> GetMyVouchers()
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);
		if (store == null) return Unauthorized();

		var vouchers = await _context.Vouchers
			.Where(v => v.StoreId == store.Id)
			.OrderByDescending(v => v.StartDate)
			.Select(v => new VoucherDto(
				v.Id,
				v.Code,
				v.Description,
				v.Value,
				v.Type.ToString(), // Convert Enum to String ("Percentage" or "FixedAmount")
				v.EndDate
			))
			.ToListAsync();

		return Ok(vouchers);
	}

	// --- FEATURE: ABANDONED CART RECOVERY (Seller View) ---

	[HttpGet("abandoned-carts")]
	public async Task<IActionResult> GetAbandonedCarts()
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);

		var cutoff = DateTime.UtcNow.AddHours(-24);

		// Find carts containing THIS store's products that are older than 24h
		var leads = await _context.Carts
			.Include(c => c.Items)
			.ThenInclude(i => i.Product)
			.Where(c => c.LastUpdated < cutoff && c.Items.Any(i => i.Product.StoreId == store!.Id))
			.Select(c => new
			{
				c.UserId,
				// Only show items from this store
				Items = c.Items.Where(i => i.Product.StoreId == store!.Id).Select(i => new
				{
					i.Product.Name,
					i.Quantity,
					i.Product.MainImageUrl
				}),
				AbandonedAt = c.LastUpdated
			})
			.ToListAsync();

		return Ok(leads);
	}

	[HttpPost("recover")]
	public async Task<IActionResult> SendRecoveryOffer([FromBody] SendTargetedOfferDto request)
	{
		// Logic: Similar to "Nudge", but targets a specific User ID from the abandoned cart list
		// 1. Generate Voucher
		// 2. Send Notification: "You left items in your cart! Finish buying with code RECOVER10"
		return Ok("Recovery offer sent.");
	}
}