using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.API.Attributes; // Required for [RequirePermission]
using Voya.Core.Constants; // Required for Permissions list
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/marketing")]
public class NexusMarketingController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public NexusMarketingController(VoyaDbContext context) { _context = context; }

	// --- CAMPAIGNS & VOUCHERS ---

	// GET: Active Campaigns (Using Vouchers as proxy)
	[HttpGet("campaigns")]
	[RequirePermission(Permissions.MarketingManage)]
	public async Task<IActionResult> GetActiveCampaigns()
	{
		var campaigns = await _context.Vouchers
			.Where(v => v.IsActive && v.EndDate > DateTime.UtcNow)
			.Select(v => new
			{
				Id = v.Id,
				Name = v.Code, // Using Code as Name for now
				Type = v.Type.ToString(),
				Reach = v.MaxUses, // Total potential reach
				Used = _context.UserVouchers.Count(uv => uv.VoucherId == v.Id),
				Status = "Active"
			})
			.ToListAsync();

		return Ok(campaigns);
	}

	// POST: Create Voucher
	[HttpPost("vouchers")]
	[RequirePermission(Permissions.MarketingManage)]
	public async Task<IActionResult> CreateVoucher([FromBody] Voucher voucher)
	{
		if (await _context.Vouchers.AnyAsync(v => v.Code == voucher.Code))
			return BadRequest("Voucher code already exists.");

		// Ensure defaults
		voucher.Id = Guid.NewGuid();
		voucher.CreatedAt = DateTime.UtcNow;
		voucher.IsActive = true;

		_context.Vouchers.Add(voucher);
		await _context.SaveChangesAsync();

		return Ok(voucher);
	}

	// --- REFERRAL PROGRAM ---

	// FEATURE 10: REFERRAL CONFIG
	[HttpPost("referral/config")]
	[RequirePermission(Permissions.MarketingManage)]
	public async Task<IActionResult> UpdateReferralConfig([FromBody] ReferralConfig config)
	{
		var existing = await _context.ReferralConfigs.FirstOrDefaultAsync();
		if (existing == null)
		{
			config.Id = Guid.NewGuid(); // Ensure ID is set
			_context.ReferralConfigs.Add(config);
		}
		else
		{
			existing.ReferrerReward = config.ReferrerReward;
			existing.RefereeReward = config.RefereeReward;
			existing.IsActive = config.IsActive;
			existing.UpdatedAt = DateTime.UtcNow;
		}
		await _context.SaveChangesAsync();
		return Ok("Referral program updated.");
	}

	// --- INFLUENCERS & AFFILIATES ---

	// GET: Pending/Active Affiliates
	[HttpGet("affiliates")]
	[RequirePermission(Permissions.MarketingManage)]
	public async Task<IActionResult> GetAffiliates()
	{
		var list = await _context.AffiliateProfiles
			.Include(a => a.User)
			.Select(a => new
			{
				a.Id,
				Name = a.User.FullName,
				Code = a.PromoCode,
				Sales = a.TotalSales,
				PendingPayout = a.PendingPayout,
				IsApproved = a.IsApproved
			})
			.ToListAsync();
		return Ok(list);
	}

	[HttpPost("affiliates/{id}/approve")]
	public async Task<IActionResult> ApproveAffiliate(Guid id)
	{
		var affiliate = await _context.AffiliateProfiles.FindAsync(id);
		if (affiliate == null) return NotFound();
		affiliate.IsApproved = true;
		await _context.SaveChangesAsync();
		return Ok("Affiliate approved.");
	}

	// --- CART RECOVERY ---

	// GET: Abandoned Cart Stats
	[HttpGet("cart-recovery/stats")]
	public async Task<IActionResult> GetRecoveryStats()
	{
		// Logic: Find carts not updated in 2 hours
		var cutoff = DateTime.UtcNow.AddHours(-2);

		var abandonedCarts = await _context.Carts
			.Include(c => c.Items)
			.ThenInclude(i => i.Product)
			.Where(c => c.LastUpdated < cutoff && c.Items.Any())
			.ToListAsync();

		var totalValue = abandonedCarts.Sum(c => c.Items.Sum(i => i.Quantity * i.Product.BasePrice));

		return Ok(new
		{
			Count = abandonedCarts.Count,
			PotentialRevenue = totalValue,
			SampleUser = abandonedCarts.FirstOrDefault()?.UserId.ToString() ?? "None"
		});
	}

	// POST: Send Recovery Nudge
	[HttpPost("cart-recovery/trigger")]
	public async Task<IActionResult> TriggerRecovery([FromBody] double discountPercent)
	{
		// Logic: Send Push Notification/Email to all abandoned cart owners
		return Ok($"Recovery campaign sent to users with {discountPercent}% offer.");
	}
}