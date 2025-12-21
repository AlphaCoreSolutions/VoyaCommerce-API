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

	// 1. GET ALL (Filterable)
	// Supports guests viewing auctions
	[HttpGet]
	[AllowAnonymous]
	public async Task<IActionResult> GetAuctions([FromQuery] string filter = "live")
	{
		var query = _context.Auctions
			.Include(a => a.Product)
			.AsQueryable();

		if (filter == "live")
		{
			query = query.Where(a => a.Status == AuctionStatus.Active && a.EndTime > DateTime.UtcNow);
		}
		else if (filter == "upcoming")
		{
			query = query.Where(a => a.Status == AuctionStatus.Active && a.StartTime > DateTime.UtcNow);
		}
		else if (filter == "ended")
		{
			query = query.Where(a => a.Status == AuctionStatus.Ended || a.Status == AuctionStatus.Sold);
		}

		var auctions = await query
			.OrderBy(a => a.EndTime)
			.Select(a => new
			{
				a.Id,
				ProductName = a.Product.Name,
				ImageUrl = a.Product.MainImageUrl,
				CurrentPrice = a.CurrentHighestBid,
				a.StartTime,
				a.EndTime,
				TotalBids = a.Bids.Count,
				Status = a.Status.ToString()
			})
			.ToListAsync();

		return Ok(auctions);
	}

	// 2. GET DETAILS
	[HttpGet("{id}")]
	[AllowAnonymous]
	public async Task<IActionResult> GetAuctionDetails(Guid id)
	{
		var auction = await _context.Auctions
			.Include(a => a.Product)
			.Include(a => a.Bids).ThenInclude(b => b.User) // Include bidders for history
			.FirstOrDefaultAsync(a => a.Id == id);

		if (auction == null) return NotFound();

		return Ok(new
		{
			auction.Id,
			Product = new { auction.Product.Name, auction.Product.Description, auction.Product.MainImageUrl },
			auction.StartPrice,
			auction.CurrentHighestBid,
			auction.StartTime,
			auction.EndTime,
			Status = auction.Status.ToString(),
			Bids = auction.Bids.OrderByDescending(b => b.Amount).Take(10).Select(b => new
			{
				b.Amount,
				UserName = b.User.FullName.Length > 2
					? $"{b.User.FullName.Substring(0, 1)}***{b.User.FullName.Substring(b.User.FullName.Length - 1)}"
					: "Anonymous", // Privacy masking
				Time = b.PlacedAt
			})
		});
	}

	// 3. CREATE AUCTION (Admin/Seller Only)
	[HttpPost]
	public async Task<IActionResult> CreateAuction([FromBody] CreateAuctionRequest request)
	{
		var userId = GetUserId();

		// Optional: Add Role Check here
		// if (!User.IsInRole("Admin") && !User.IsInRole("Seller")) return Forbid();

		var product = await _context.Products.FindAsync(request.ProductId);
		if (product == null) return NotFound("Product not found");

		var auction = new Auction
		{
			ProductId = request.ProductId,
			SellerId = userId,
			StartPrice = request.StartPrice,
			CurrentHighestBid = request.StartPrice, // Bid starts at base price
			StartTime = request.StartTime,
			EndTime = request.EndTime,
			Status = AuctionStatus.Active
		};

		_context.Auctions.Add(auction);
		await _context.SaveChangesAsync();

		return Ok(new { AuctionId = auction.Id, Message = "Auction created successfully." });
	}

	// 4. PLACE BID (Critical Logic)
	[HttpPost("{id}/bid")]
	public async Task<IActionResult> PlaceBid(Guid id, [FromBody] BidRequest request)
	{
		var userId = GetUserId();

		// Use Transaction to prevent race conditions (Double Bidding)
		using var transaction = await _context.Database.BeginTransactionAsync();
		try
		{
			// Lock the row if using SQL Server specific hints, otherwise EF concurrency token handles basic checks
			var auction = await _context.Auctions.FindAsync(id);

			if (auction == null) return NotFound();
			if (auction.Status != AuctionStatus.Active) return BadRequest("Auction is not active.");
			if (DateTime.UtcNow > auction.EndTime) return BadRequest("Auction has ended.");

			// Validate Amount
			if (request.Amount <= auction.CurrentHighestBid)
				return BadRequest($"Bid must be higher than {auction.CurrentHighestBid}");

			// Create Bid
			var bid = new AuctionBid
			{
				AuctionId = id,
				UserId = userId,
				Amount = request.Amount,
				PlacedAt = DateTime.UtcNow
			};

			// Update Auction State
			auction.CurrentHighestBid = request.Amount;
			auction.CurrentWinnerId = userId;

			// Anti-Sniping Rule: 
			// If bid is placed in the last 30 seconds, extend the auction by 1 minute.
			// This prevents bots from stealing the item at the last millisecond.
			if ((auction.EndTime - DateTime.UtcNow).TotalSeconds < 30)
			{
				auction.EndTime = auction.EndTime.AddMinutes(1);
			}

			_context.AuctionBids.Add(bid);
			await _context.SaveChangesAsync();
			await transaction.CommitAsync();

			// REAL-TIME NOTIFICATION (SignalR)
			// Notify everyone watching this auction
			await _hubContext.Clients.Group($"Auction-{id}").SendAsync("NewBid", new
			{
				Amount = request.Amount,
				NewEndTime = auction.EndTime,
				User = "New Bidder"
			});

			return Ok(new { Message = "Bid placed successfully!", NewEndTime = auction.EndTime });
		}
		catch (Exception ex)
		{
			await transaction.RollbackAsync();
			// Log error
			return StatusCode(500, "Error processing bid: " + ex.Message);
		}
	}
}

// --- DTOs ---

public class CreateAuctionRequest
{
	public Guid ProductId { get; set; }
	public decimal StartPrice { get; set; }
	public DateTime StartTime { get; set; }
	public DateTime EndTime { get; set; }
}

public class BidRequest
{
	public decimal Amount { get; set; }
}