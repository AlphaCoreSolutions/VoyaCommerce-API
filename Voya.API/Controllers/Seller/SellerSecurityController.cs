using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

[Authorize]
[ApiController]
[Route("api/v1/seller/security")]
public class SellerSecurityController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public SellerSecurityController(VoyaDbContext context) { _context = context; }

	// --- BLACKLIST REQUEST ---
	[HttpPost("blacklist")]
	public async Task<IActionResult> RequestBlacklist([FromBody] BlacklistRequest request)
	{
		// ... (Validate Store Ownership) ...
		request.Status = BlacklistStatus.Pending;
		_context.BlacklistRequests.Add(request);
		await _context.SaveChangesAsync();
		return Ok("Blacklist request sent to admin.");
	}

	// --- B2B TIERS (Wholesale) ---
	[HttpPost("pricing/tiers")]
	public async Task<IActionResult> AddPricingTier([FromBody] ProductTierPrice tier)
	{
		_context.ProductTierPrices.Add(tier);
		await _context.SaveChangesAsync();
		return Ok("Wholesale pricing tier added.");
	}
}