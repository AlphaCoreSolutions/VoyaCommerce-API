using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Voya.API.Hubs;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/auctions")]
public class AuctionsController : ControllerBase
{
	private readonly VoyaDbContext _context;
	private readonly IHubContext<LiveHub> _hubContext;

	public AuctionsController(VoyaDbContext context, IHubContext<LiveHub> hubContext)
	{
		_context = context;
		_hubContext = hubContext;
	}

	private Guid GetUserId()
	{
		var idClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
		if (idClaim == null) throw new UnauthorizedAccessException();
		return Guid.Parse(idClaim.Value);
	}

	// ==========================================
	// 1. PUBLIC VIEW (Browsing)
	// ==========================================

	[HttpGet]
	[AllowAnonymous]
	public async Task<IActionResult> GetAuctions([FromQuery] string filter = "live")
	{
		var query = _context.Auctions.AsQueryable();

		// Only show Approved/Visible auctions to public
		if (filter == "live")
		{
			query = query.Where(a => a.Status == AuctionStatus.Active && a.EndTime > DateTime.UtcNow);
		}
		else if (filter == "upcoming")
		{
			// Show approved upcoming auctions
			query = query.Where(a => (a.Status == AuctionStatus.Upcoming || a.Status == AuctionStatus.Active)
									 && a.StartTime > DateTime.UtcNow);
		}
		// ... handled other filters like 'ended'

		var auctions = await query
			.OrderBy(a => a.EndTime) // Ending soonest first
			.Select(a => new
			{
				a.Id,
				a.Title,            // Changed from Product.Name
				ImageUrl = a.MainImageUrl, // Changed from Product.MainImage
				CurrentPrice = a.CurrentHighestBid > 0 ? a.CurrentHighestBid : a.StartPrice,
				a.StartTime,
				a.EndTime,
				TotalBids = a.Bids.Count,
				Status = a.Status.ToString()
			})
			.ToListAsync();

		return Ok(auctions);
	}

	[HttpGet("{id}")]
	[AllowAnonymous]
	public async Task<IActionResult> GetAuctionDetails(Guid id)
	{
		var auction = await _context.Auctions
			.Include(a => a.Bids).ThenInclude(b => b.User)
			.Include(a => a.Seller)
			.FirstOrDefaultAsync(a => a.Id == id);

		if (auction == null) return NotFound();

		// Check if current user has a reminder set (if logged in)
		bool hasReminder = false;
		try
		{
			var userId = GetUserId();
			hasReminder = await _context.AuctionReminders
				.AnyAsync(r => r.AuctionId == id && r.UserId == userId);
		}
		catch { /* User not logged in, ignore */ }

		return Ok(new
		{
			auction.Id,
			auction.Title,
			auction.Description,
			auction.MainImageUrl,
			auction.ImageGallery,
			SellerName = auction.Seller.FullName,
			SellerAvatar = auction.Seller.AvatarUrl,
			auction.StartPrice,
			auction.CurrentHighestBid,
			auction.StartTime,
			auction.EndTime,
			Status = auction.Status.ToString(),
			HasReminder = hasReminder, // UI needs this for the button state
			Bids = auction.Bids.OrderByDescending(b => b.Amount).Take(10).Select(b => new
			{
				b.Amount,
				UserName = MaskName(b.User.FullName),
				Time = b.PlacedAt
			})
		});
	}

	private string MaskName(string name) =>
		name.Length > 2 ? $"{name[0]}***{name[^1]}" : "Anonymous";

	// ==========================================
	// 2. SELLER ACTIONS (Create)
	// ==========================================

	[HttpPost]
	public async Task<IActionResult> CreateAuction([FromBody] CreateAuctionRequest request)
	{
		var userId = GetUserId();

		// Validation
		if (request.StartTime < DateTime.UtcNow)
			return BadRequest("Start time cannot be in the past.");

		if (request.EndTime <= request.StartTime)
			return BadRequest("End time must be after start time.");

		var auction = new Auction
		{
			SellerId = userId,
			Title = request.Title,
			Description = request.Description,
			MainImageUrl = request.MainImageUrl,
			ImageGallery = request.ImageGallery ?? new List<string>(),
			StartPrice = request.StartPrice,
			CurrentHighestBid = request.StartPrice, // Initial bid baseline
			ReservePrice = request.ReservePrice,
			StartTime = request.StartTime,
			EndTime = request.EndTime,

			// CRITICAL: New auctions go to PendingApproval, not Active immediately
			Status = AuctionStatus.PendingApproval
		};

		_context.Auctions.Add(auction);
		await _context.SaveChangesAsync();

		// Notify Admins? (Optional: SignalR or Email)

		return Ok(new
		{
			AuctionId = auction.Id,
			Message = "Auction submitted for review. You will be notified when it is approved."
		});
	}

	// ==========================================
	// 3. BUYER ACTIONS (Bid & Remind)
	// ==========================================

	[HttpPost("{id}/bid")]
	public async Task<IActionResult> PlaceBid(Guid id, [FromBody] BidRequest request)
	{
		var userId = GetUserId();

		using var transaction = await _context.Database.BeginTransactionAsync();
		try
		{
			var auction = await _context.Auctions.FindAsync(id);

			if (auction == null) return NotFound();
			if (auction.Status != AuctionStatus.Active) return BadRequest("Auction is not live.");
			if (DateTime.UtcNow > auction.EndTime) return BadRequest("Auction has ended.");
			if (auction.SellerId == userId) return BadRequest("You cannot bid on your own auction.");

			// Bid Validation
			// Must be higher than current highest OR equal to start price if no bids yet
			decimal minBid = auction.CurrentHighestBid > 0
				? auction.CurrentHighestBid + 1.00m // Min increment logic (e.g. $1)
				: auction.StartPrice;

			if (request.Amount < minBid)
				return BadRequest($"Bid must be at least {minBid:C}");

			// Create Bid
			var bid = new AuctionBid
			{
				AuctionId = id,
				UserId = userId,
				Amount = request.Amount,
				PlacedAt = DateTime.UtcNow
			};

			// Update Auction
			auction.CurrentHighestBid = request.Amount;
			auction.CurrentWinnerId = userId;

			// Anti-Sniping (Extend by 1 min if < 30s left)
			if ((auction.EndTime - DateTime.UtcNow).TotalSeconds < 30)
			{
				auction.EndTime = auction.EndTime.AddMinutes(1);
			}

			_context.AuctionBids.Add(bid);
			await _context.SaveChangesAsync();
			await transaction.CommitAsync();

			// Real-time Update
			await _hubContext.Clients.Group($"Auction-{id}").SendAsync("NewBid", new
			{
				Amount = request.Amount,
				NewEndTime = auction.EndTime,
				User = "New Bidder"
			});

			return Ok(new { Message = "Bid placed!", NewEndTime = auction.EndTime });
		}
		catch (Exception ex)
		{
			await transaction.RollbackAsync();
			return StatusCode(500, ex.Message);
		}
	}

	// NEW: "Remind Me" Feature
	[HttpPost("{id}/remind")]
	public async Task<IActionResult> ToggleReminder(Guid id)
	{
		var userId = GetUserId();

		var existing = await _context.AuctionReminders
			.FirstOrDefaultAsync(r => r.AuctionId == id && r.UserId == userId);

		if (existing != null)
		{
			_context.AuctionReminders.Remove(existing);
			await _context.SaveChangesAsync();
			return Ok(new { IsReminding = false, Message = "Reminder removed." });
		}

		var auction = await _context.Auctions.FindAsync(id);
		if (auction == null) return NotFound();

		_context.AuctionReminders.Add(new AuctionReminder
		{
			AuctionId = id,
			UserId = userId
		});

		await _context.SaveChangesAsync();
		return Ok(new { IsReminding = true, Message = "We will notify you when this starts!" });
	}
}

// ==========================================
// 4. UPDATED DTOs
// ==========================================

public class CreateAuctionRequest
{
	public string Title { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public string MainImageUrl { get; set; } = string.Empty;
	public List<string>? ImageGallery { get; set; }

	public decimal StartPrice { get; set; }
	public decimal ReservePrice { get; set; }

	public DateTime StartTime { get; set; }
	public DateTime EndTime { get; set; }
}

public class BidRequest
{
	public decimal Amount { get; set; }
}