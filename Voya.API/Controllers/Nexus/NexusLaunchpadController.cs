using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Transactions;
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

	public NexusLaunchpadController(VoyaDbContext context)
	{
		_context = context;
	}

	// ==================================================================================
	// 1. GET CAMPAIGNS (Admin View)
	// ==================================================================================
	[HttpGet]
	[RequirePermission(Permissions.CampaignsView)]
	public async Task<IActionResult> GetActiveCampaigns()
	{
		// Return mock data if DB is empty for testing UI
		if (!await _context.Campaigns.AnyAsync())
		{
			return Ok(new List<object>
			{
				new {
					Id = Guid.NewGuid(),
					Title = "Anti-Gravity Running Shoes",
					CreatorName = "FutureWear Inc.",
					GoalAmount = 100000,
					CurrentAmount = 120000,
					EndDate = DateTime.UtcNow.AddDays(5),
					Status = "Funded",
					IsReadyForPayout = true
				},
				new {
					Id = Guid.NewGuid(),
					Title = "Smart Coffee Mug",
					CreatorName = "HomeTech",
					GoalAmount = 50000,
					CurrentAmount = 15000,
					EndDate = DateTime.UtcNow.AddDays(12),
					Status = "Active",
					IsReadyForPayout = false
				}
			});
		}

		var campaigns = await _context.Campaigns
			.Include(c => c.Creator)
			.OrderByDescending(c => c.EndDate)
			.Select(c => new
			{
				c.Id,
				c.Title,
				CreatorName = c.Creator.FullName,
				c.GoalAmount,
				c.CurrentAmount,
				c.EndDate,
				Status = c.Status.ToString(),
				// Helper logic: Is it safe to pay them?
				IsReadyForPayout = c.Status == CampaignStatus.Funded || (c.Status == CampaignStatus.Active && c.CurrentAmount >= c.GoalAmount)
			})
			.ToListAsync();

		return Ok(campaigns);
	}

	// ==================================================================================
	// 2. MANUAL FUND RELEASE
	// ==================================================================================
	[HttpPost("{id}/release-funds")]
	[RequirePermission(Permissions.FinancePayout)]
	public async Task<IActionResult> ReleaseFunds(Guid id)
	{
		// 1. Get Campaign and Creator
		var campaign = await _context.Campaigns
			.Include(c => c.Creator)
			.FirstOrDefaultAsync(c => c.Id == id);

		if (campaign == null) return NotFound("Campaign not found.");

		// 2. Validation
		if (campaign.Status == CampaignStatus.FundsReleased)
			return BadRequest("Funds have already been released.");

		// Allow Manual Override: Even if status is 'Active', if they met the goal, admin can force payout.
		bool goalMet = campaign.CurrentAmount >= campaign.GoalAmount;
		if (!goalMet && campaign.Status != CampaignStatus.Funded)
			return BadRequest("Campaign has not met its funding goal.");

		// 3. Financials (Platform Fee Calculation)
		decimal platformFeeRate = 0.08m; // Crowdfunding fee is usually higher (e.g. 8%)
		decimal grossAmount = campaign.CurrentAmount;
		decimal fee = grossAmount * platformFeeRate;
		decimal netPayout = grossAmount - fee;

		// 4. Execute Transfer (Wallet System)
		var creator = campaign.Creator;
		if (creator != null)
		{
			creator.WalletBalance += netPayout;

			// Record Transaction
			var transaction = new WalletTransaction
			{
				UserId = creator.Id,
				StoreId = null, // Typically Launchpad creators are users, not Stores
				Amount = netPayout,
				Type = TransactionType.Sale, // Using 'Sale' as revenue type
				Status = Core.Entities.TransactionStatus.Completed,
				Description = $"Launchpad Payout: {campaign.Title} (minus {fee:C} fee)",
				Date = DateTime.UtcNow
			};

			_context.WalletTransactions.Add(transaction);
		}

		// 5. Update Status
		campaign.Status = CampaignStatus.FundsReleased;
		await _context.SaveChangesAsync();

		return Ok(new
		{
			Message = "Funds released to creator wallet.",
			Payout = netPayout,
			Fee = fee
		});
	}
}