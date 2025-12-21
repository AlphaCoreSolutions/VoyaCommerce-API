using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Seller;

[ApiController]
[Route("api/v1/seller/wallet")]
public class SellerWalletController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public SellerWalletController(VoyaDbContext context) { _context = context; }

	private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

	// FEATURE 6: FINANCIALS

	[HttpGet]
	[HttpGet]
	public async Task<IActionResult> GetWallet()
	{
		var userId = GetUserId();

		// Check if user is a Store Owner
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);

		// Logic: Get transactions for Store OR directly for User
		var query = _context.WalletTransactions.AsQueryable();

		if (store != null)
		{
			// Get both Store transactions AND personal auction transactions
			query = query.Where(t => t.StoreId == store.Id || t.UserId == userId);
		}
		else
		{
			// Regular user (Auction Seller only)
			query = query.Where(t => t.UserId == userId);
		}

		var transactions = await query
			.OrderByDescending(t => t.Date)
			.Take(50)
			.ToListAsync();

		var balance = transactions.Sum(t => t.Amount);

		return Ok(new { Balance = balance, RecentTransactions = transactions });
	}

	[HttpPost("withdraw")]
	public async Task<IActionResult> RequestPayout([FromBody] decimal amount)
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);

		// 1. Check Balance
		var currentBalance = await _context.WalletTransactions
			.Where(t => t.StoreId == store!.Id)
			.SumAsync(t => t.Amount);

		if (amount > currentBalance) return BadRequest("Insufficient funds.");

		// 2. Create Payout Request
		_context.PayoutRequests.Add(new PayoutRequest
		{
			StoreId = store!.Id,
			Amount = amount,
			BankDetailsJson = "{ 'Bank': 'Arab Bank', 'IBAN': 'JO...' }"
		});

		// 3. Deduct from Wallet immediately (Pending State)
		_context.WalletTransactions.Add(new WalletTransaction
		{
			StoreId = store.Id,
			Amount = -amount,
			Type = TransactionType.Payout,
			Description = "Payout Request Pending"
		});

		await _context.SaveChangesAsync();
		return Ok("Withdrawal requested.");
	}
}