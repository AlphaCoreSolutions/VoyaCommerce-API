using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/split-bill")]
public class SplitBillController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public SplitBillController(VoyaDbContext context)
	{
		_context = context;
	}

	private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

	// 1. INITIATE SPLIT BILL (Manager Only)
	[HttpPost("initiate/{cartId}")]
	public async Task<IActionResult> InitiateSplit(Guid cartId)
	{
		var userId = GetUserId();

		// Load Cart, Items, and Members
		var cart = await _context.Carts
			.Include(c => c.Items).ThenInclude(i => i.Product)
			.Include(c => c.Members)
			.FirstOrDefaultAsync(c => c.Id == cartId);

		if (cart == null) return NotFound("Cart not found.");

		// Validation: Only Manager can start split
		// (Assuming logic: Cart Owner is Manager)
		if (cart.UserId != userId && !cart.Members.Any(m => m.UserId == userId && m.Role == "Manager"))
		{
			return StatusCode(403, "Only the cart manager can initiate a split bill.");
		}

		if (!cart.Items.Any()) return BadRequest("Cart is empty.");

		// Check if active split already exists
		var existingSplit = await _context.SplitBills
			.FirstOrDefaultAsync(sb => sb.CartId == cartId && sb.Status != SplitBillStatus.Cancelled && sb.Status != SplitBillStatus.Completed);

		if (existingSplit != null) return BadRequest("A split bill is already active for this cart.");

		// === CALCULATION LOGIC ===
		// 1. Calculate Individual Items (User pays for what they added)
		var userTotals = new Dictionary<Guid, decimal>();

		// Initialize all members with 0
		userTotals[cart.UserId] = 0;
		foreach (var member in cart.Members) userTotals[member.UserId] = 0;

		foreach (var item in cart.Items)
		{
			var cost = item.Product.BasePrice * item.Quantity;

			// If AddedByUserId is valid member, assign cost. Else assign to Manager.
			if (userTotals.ContainsKey(item.AddedByUserId))
			{
				userTotals[item.AddedByUserId] += cost;
			}
			else
			{
				userTotals[cart.UserId] += cost; // Fallback to Owner
			}
		}

		// Create the Split Bill Header
		var splitBill = new SplitBill
		{
			CartId = cart.Id,
			InitiatorUserId = userId,
			TotalAmount = userTotals.Values.Sum(),
			Status = SplitBillStatus.Pending
		};

		// Create Shares
		foreach (var kvp in userTotals)
		{
			// Only add share if amount > 0
			if (kvp.Value > 0)
			{
				splitBill.Shares.Add(new SplitBillShare
				{
					UserId = kvp.Key,
					AmountDue = kvp.Value,
					Status = ShareStatus.Pending
				});
			}
		}

		_context.SplitBills.Add(splitBill);
		await _context.SaveChangesAsync();

		// Notification Logic here: "Pay your share of $X!"

		return Ok(new { SplitBillId = splitBill.Id, Message = "Split bill initiated. Members notified." });
	}

	// 2. GET STATUS
	[HttpGet("{id}")]
	public async Task<IActionResult> GetStatus(Guid id)
	{
		var split = await _context.SplitBills
			.Include(sb => sb.Shares).ThenInclude(s => s.User)
			.FirstOrDefaultAsync(sb => sb.Id == id);

		if (split == null) return NotFound();

		return Ok(new
		{
			split.Id,
			split.TotalAmount,
			split.Status,
			Shares = split.Shares.Select(s => new
			{
				s.UserId,
				UserName = s.User.FullName,
				s.AmountDue,
				s.AmountPaid,
				Status = s.Status.ToString(),
				IsMe = s.UserId == GetUserId()
			})
		});
	}

	// 3. PAY MY SHARE
	[HttpPost("{id}/pay")]
	public async Task<IActionResult> PayShare(Guid id)
	{
		var userId = GetUserId();
		var split = await _context.SplitBills
			.Include(sb => sb.Shares)
			.FirstOrDefaultAsync(sb => sb.Id == id);

		if (split == null) return NotFound();
		if (split.Status == SplitBillStatus.Completed || split.Status == SplitBillStatus.Cancelled)
			return BadRequest("This bill is no longer active.");

		var myShare = split.Shares.FirstOrDefault(s => s.UserId == userId);
		if (myShare == null) return BadRequest("You are not part of this split bill.");
		if (myShare.Status == ShareStatus.Paid) return Ok("Already paid.");

		// === PAYMENT INTEGRATION HERE ===
		// In real app: Validate Stripe PaymentIntent or check Wallet Balance
		// For MVP: We assume payment success immediately

		myShare.Status = ShareStatus.Paid;
		myShare.AmountPaid = myShare.AmountDue;
		myShare.PaidAt = DateTime.UtcNow;

		// Check if EVERYONE has paid
		bool allPaid = split.Shares.All(s => s.Status == ShareStatus.Paid);

		if (allPaid)
		{
			split.Status = SplitBillStatus.FullyPaid;
			// TRIGGER AUTO-CHECKOUT? 
			// Usually we call the OrderService internally here to finalize the order.
			// For now, we update status so the Manager sees "Ready" and clicks "Finalize".
		}
		else
		{
			split.Status = SplitBillStatus.Collecting;
		}

		await _context.SaveChangesAsync();
		return Ok(new { Message = "Payment successful", IsFullyPaid = allPaid });
	}

	// 4. CANCEL (Manager Only)
	[HttpPost("{id}/cancel")]
	public async Task<IActionResult> CancelSplit(Guid id)
	{
		var userId = GetUserId();
		var split = await _context.SplitBills.FindAsync(id);

		if (split == null) return NotFound();
		if (split.InitiatorUserId != userId) return StatusCode(403);

		split.Status = SplitBillStatus.Cancelled;
		// Logic: Refund anyone who already paid

		await _context.SaveChangesAsync();
		return Ok("Split bill cancelled.");
	}
}