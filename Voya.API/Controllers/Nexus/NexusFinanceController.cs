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
[Route("api/v1/nexus/finance")]
public class NexusFinanceController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public NexusFinanceController(VoyaDbContext context)
	{
		_context = context;
	}

	// ==================================================================================
	// 1. PENDING VENDOR PAYOUTS (Manual Approval Workflow)
	// ==================================================================================

	[HttpGet("payouts/pending")]
	[RequirePermission(Permissions.FinanceView)]
	public async Task<IActionResult> GetPendingPayouts()
	{
		// Join with Store to show who is asking for money
		var requests = await _context.PayoutRequests
			.Where(p => !p.IsProcessed)
			.OrderBy(p => p.RequestedAt)
			.Select(p => new
			{
				p.Id,
				StoreName = p.StoreId != null
					? _context.Stores.Where(s => s.Id == p.StoreId).Select(s => s.Name).FirstOrDefault()
					: "Individual User",
				p.Amount,
				p.RequestedAt,
				p.BankDetailsJson
			})
			.ToListAsync();

		return Ok(requests);
	}

	[HttpPost("payouts/{id}/approve")]
	[RequirePermission(Permissions.FinancePayout)]
	public async Task<IActionResult> ApprovePayout(Guid id)
	{
		var request = await _context.PayoutRequests.FindAsync(id);
		if (request == null) return NotFound("Request not found.");
		if (request.IsProcessed) return BadRequest("Already processed.");

		// 1. Mark Request as Processed
		request.IsProcessed = true;

		// 2. Update the Wallet Transaction Status
		// Find the "Pending" debit transaction we created when they requested withdrawal
		// Note: In SellerWalletController, we created a transaction with Type=Payout. 
		// We should ideally link PayoutRequest ID to WalletTransaction ID, but for now we query by matching params.
		var pendingTx = await _context.WalletTransactions
			.FirstOrDefaultAsync(t =>
				t.StoreId == request.StoreId &&
				t.Amount == -request.Amount &&
				t.Type == TransactionType.Payout &&
				t.Description == "Payout Request Pending"); // Matches string from SellerWalletController

		if (pendingTx != null)
		{
			pendingTx.Description = "Payout Processed (Sent to Bank)";
			// We don't change status because it was already deducted, but we confirm it here.
		}

		await _context.SaveChangesAsync();

		// In a real app: Trigger External Bank API (Stripe Connect / Wise) here.

		return Ok(new { Message = $"Payout of {request.Amount:C} approved and processed." });
	}

	// ==================================================================================
	// 2. AUTOMATION CONTROL (Kill Switch)
	// ==================================================================================

	[HttpGet("settings")]
	[RequirePermission(Permissions.SettingsView)]
	public async Task<IActionResult> GetFinanceSettings()
	{
		var settings = await _context.GlobalSettings
			.Where(s => s.Key.StartsWith("Auctions.") || s.Key.StartsWith("Launchpad."))
			.ToListAsync();

		return Ok(new
		{
			AuctionAutoRelease = settings.FirstOrDefault(s => s.Key == "Auctions.AutoReleaseEnabled")?.Value ?? "True",
			LaunchpadAutoRelease = settings.FirstOrDefault(s => s.Key == "Launchpad.AutoReleaseEnabled")?.Value ?? "False"
		});
	}

	[HttpPost("toggle-auction-automation")]
	[RequirePermission(Permissions.SettingsManage)]
	public async Task<IActionResult> ToggleAuctionAutomation([FromBody] bool enable)
	{
		await UpdateGlobalSetting("Auctions.AutoReleaseEnabled", enable.ToString());
		return Ok($"Auction Auto-Payouts are now {(enable ? "ENABLED" : "DISABLED")}");
	}

	[HttpPost("toggle-launchpad-automation")]
	[RequirePermission(Permissions.SettingsManage)]
	public async Task<IActionResult> ToggleLaunchpadAutomation([FromBody] bool enable)
	{
		await UpdateGlobalSetting("Launchpad.AutoReleaseEnabled", enable.ToString());
		return Ok($"Launchpad Auto-Payouts are now {(enable ? "ENABLED" : "DISABLED")}");
	}

	private async Task UpdateGlobalSetting(string key, string value)
	{
		var setting = await _context.GlobalSettings.FirstOrDefaultAsync(s => s.Key == key);
		if (setting == null)
		{
			_context.GlobalSettings.Add(new GlobalSetting
			{
				Key = key,
				Value = value,
				Description = "Finance Automation Switch"
			});
		}
		else
		{
			setting.Value = value;
			setting.UpdatedAt = DateTime.UtcNow;
		}
		await _context.SaveChangesAsync();
	}
}