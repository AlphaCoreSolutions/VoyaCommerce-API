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
[Route("api/v1/nexus/launchpad")]
public class NexusLaunchpadController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public NexusLaunchpadController(VoyaDbContext context) { _context = context; }

	[HttpGet]
	public async Task<IActionResult> GetActiveCampaigns()
	{
		// Return mock data if DB is empty for testing
		if (!await _context.Campaigns.AnyAsync())
		{
			return Ok(new List<object>
			{
				new { Id = Guid.NewGuid(), Title = "Anti-Gravity Running Shoes", CreatorName = "FutureWear Inc.", GoalAmount = 100000, CurrentAmount = 120000, EndDate = DateTime.UtcNow.AddDays(5), Status = "Funded" },
				new { Id = Guid.NewGuid(), Title = "Smart Coffee Mug", CreatorName = "HomeTech", GoalAmount = 50000, CurrentAmount = 15000, EndDate = DateTime.UtcNow.AddDays(12), Status = "Active" }
			});
		}

		return Ok(await _context.Campaigns.ToListAsync());
	}

	[HttpPost("{id}/release-funds")]
	[RequirePermission(Permissions.FinancePayout)]
	public async Task<IActionResult> ReleaseFunds(Guid id)
	{
		var campaign = await _context.Campaigns.FindAsync(id);
		if (campaign == null) return NotFound();

		// Business Logic: Transfer logic goes here
		campaign.Status = CampaignStatus.FundsReleased;
		await _context.SaveChangesAsync();

		return Ok("Funds released to creator wallet.");
	}
}