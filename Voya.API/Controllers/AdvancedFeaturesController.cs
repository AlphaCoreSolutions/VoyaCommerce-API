using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR; // For real-time updates
using Microsoft.EntityFrameworkCore;
using Voya.API.Hubs;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/features")]
public class AdvancedFeaturesController : ControllerBase
{
	private readonly VoyaDbContext _context;
	private readonly IHubContext<LiveHub> _hubContext;

	public AdvancedFeaturesController(VoyaDbContext context, IHubContext<LiveHub> hubContext)
	{
		_context = context;
		_hubContext = hubContext;
	}

	private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")!.Value);

	// ==========================================
	// 1. REVERSE AUCTION (Mystery Price Drop)
	// ==========================================

	// Store Requests Auction
	[HttpPost("auction/request")]
	public async Task<IActionResult> RequestAuction(Guid productId, decimal startPrice, decimal minPrice)
	{
		var auction = new ReverseAuction
		{
			StoreId = Guid.NewGuid(), // Mock: Get real StoreID from User role
			ProductId = productId,
			StartPrice = startPrice,
			MinPrice = minPrice,
			DecrementAmount = 1.0m, // Drops by $1
			IntervalSeconds = 10,   // Every 10s
			Status = ApprovalStatus.Pending
		};
		_context.ReverseAuctions.Add(auction);
		await _context.SaveChangesAsync();
		return Ok("Auction requested. Waiting for Admin approval.");
	}

	// Admin Approves (In real app, restrict this to Admin Role)
	[HttpPut("auction/{id}/approve")]
	public async Task<IActionResult> ApproveAuction(Guid id)
	{
		var auction = await _context.ReverseAuctions.FindAsync(id);
		if (auction == null) return NotFound();

		auction.Status = ApprovalStatus.Active;
		auction.StartTime = DateTime.UtcNow; // Starts NOW
		await _context.SaveChangesAsync();

		// Notify everyone via SignalR
		await _hubContext.Clients.All.SendAsync("AuctionStarted", new { auction.Id, auction.StartPrice });

		return Ok("Auction is LIVE!");
	}

	// Get Current Price (Calculated dynamically)
	[AllowAnonymous]
	[HttpGet("auction/{id}/price")]
	public async Task<IActionResult> GetAuctionPrice(Guid id)
	{
		var auction = await _context.ReverseAuctions.FindAsync(id);
		if (auction == null || auction.Status != ApprovalStatus.Active) return BadRequest("Auction not active");

		var elapsedSeconds = (DateTime.UtcNow - auction.StartTime!.Value).TotalSeconds;
		var intervals = (int)(elapsedSeconds / auction.IntervalSeconds);
		var dropAmount = intervals * auction.DecrementAmount;

		var currentPrice = auction.StartPrice - dropAmount;
		if (currentPrice < auction.MinPrice) currentPrice = auction.MinPrice;

		return Ok(new { CurrentPrice = currentPrice, TimeElapsed = elapsedSeconds });
	}

	// User Buys (Wins)
	[Authorize]
	[HttpPost("auction/{id}/buy")]
	public async Task<IActionResult> BuyAuction(Guid id)
	{
		var userId = GetUserId();
		// Use a transaction to prevent race conditions (two people buying same millisecond)
		using var transaction = _context.Database.BeginTransaction();
		try
		{
			var auction = await _context.ReverseAuctions.FindAsync(id);
			if (auction == null || auction.Status != ApprovalStatus.Active) return BadRequest("Auction ended.");

			// Calculate final price
			var elapsedSeconds = (DateTime.UtcNow - auction.StartTime!.Value).TotalSeconds;
			var currentPrice = Math.Max(auction.MinPrice, auction.StartPrice - ((int)(elapsedSeconds / auction.IntervalSeconds) * auction.DecrementAmount));

			// Mark sold
			auction.Status = ApprovalStatus.Completed;
			auction.WinnerUserId = userId;
			auction.EndedTime = DateTime.UtcNow;

			await _context.SaveChangesAsync();
			await transaction.CommitAsync();

			return Ok(new { Message = "You won!", FinalPrice = currentPrice });
		}
		catch
		{
			return BadRequest("Someone else bought it first!");
		}
	}

	// ==========================================
	// 2. STYLE BOARDS (Collages)
	// ==========================================

	// GET: List the current user's created boards
	[HttpGet("styleboards/mine")]
	public async Task<IActionResult> GetMyStyleBoards()
	{
		var userId = GetUserId();

		var boards = await _context.StyleBoards
			.Where(b => b.CreatorUserId == userId)
			.Select(b => new
			{
				b.Id,
				b.Name,
				b.SalesGenerated,
				// Fetch the product images to show a preview collage
				PreviewImages = _context.Products
					.Where(p => b.ProductIds.Contains(p.Id))
					.Select(p => p.MainImageUrl)
					.Take(4) // Just get 4 for the preview grid
					.ToList(),
				ProductCount = b.ProductIds.Count
			})
			.ToListAsync();

		return Ok(boards);
	}

	[HttpPost("styleboard")]
	public async Task<IActionResult> CreateStyleBoard(string name, List<Guid> productIds)
	{
		var board = new StyleBoard
		{
			CreatorUserId = GetUserId(),
			Name = name,
			ProductIds = productIds
		};
		_context.StyleBoards.Add(board);
		await _context.SaveChangesAsync();
		return Ok("Style Board created!");
	}

	// Simulate purchasing from a board
	[HttpPost("styleboard/{id}/purchase-attribution")]
	public async Task<IActionResult> BuyFromBoard(Guid id)
	{
		var board = await _context.StyleBoards.FindAsync(id);
		if (board == null) return NotFound();

		board.SalesGenerated++;

		// Reward Creator (Points)
		var creator = await _context.Users.FindAsync(board.CreatorUserId);
		if (creator != null)
		{
			creator.PointsBalance += 5; // Reward 5 points per sale
		}

		await _context.SaveChangesAsync();
		return Ok("Purchase attributed. Creator rewarded.");
	}

	// ==========================================
	// 3. SHAKE TO WIN
	// ==========================================
	[Authorize]
	[HttpPost("shake-win")]
	public async Task<IActionResult> ShakeToWin()
	{
		var userId = GetUserId();
		// Simple logic: 20% chance to win
		if (new Random().Next(1, 100) > 80)
		{
			var user = await _context.Users.FindAsync(userId);
			user!.PointsBalance += 10;
			await _context.SaveChangesAsync();
			return Ok(new { Won = true, Reward = "10 Points", NewBalance = user.PointsBalance });
		}
		return Ok(new { Won = false, Message = "Try again later!" });
	}

	// ==========================================
	// 4. SOCIAL GIFTING
	// ==========================================
	[HttpPost("gift/send")]
	public async Task<IActionResult> SendGift(string recipientPhone, string message)
	{
		// 1. In reality, you'd create an Order here first with "IsGift=true"
		var orderId = $"ORD-GIFT-{new Random().Next(1000, 9999)}";
		var token = Guid.NewGuid().ToString()[..8].ToUpper(); // Simple token

		var gift = new GiftOrder
		{
			SenderUserId = GetUserId(),
			RecipientPhoneNumber = recipientPhone,
			PersonalMessage = message,
			OrderId = orderId,
			ClaimToken = token
		};

		_context.GiftOrders.Add(gift);
		await _context.SaveChangesAsync();

		// 2. In reality, Send SMS via Twilio: "You got a gift! Claim at voya.app/claim/{token}"
		return Ok(new { Message = "Gift sent!", SmsText = $"You got a gift on VOYA! Code: {token}" });
	}

	[HttpPost("gift/claim")]
	public async Task<IActionResult> ClaimGift(string token)
	{
		var userId = GetUserId();
		var gift = await _context.GiftOrders.FirstOrDefaultAsync(g => g.ClaimToken == token && !g.IsClaimed);

		if (gift == null) return BadRequest("Invalid or already claimed gift.");

		gift.IsClaimed = true;
		// Logic: Link order to this user's address logic would go here

		await _context.SaveChangesAsync();
		return Ok(new { Message = "Gift claimed! It will appear in your orders." });
	}

	// ==========================================
	// 6. SHARED GROUP CART (Multi-User)
	// ==========================================
	[HttpPost("group-cart/create")]
	public async Task<IActionResult> CreateGroupCart()
	{
		var cart = new SharedCart
		{
			HostUserId = GetUserId(),
			JoinCode = new Random().Next(1000, 9999).ToString(),
			ParticipantUserIds = new List<Guid> { GetUserId() }
		};
		_context.SharedCarts.Add(cart);
		await _context.SaveChangesAsync();
		return Ok(new { CartId = cart.Id, JoinCode = cart.JoinCode });
	}

	[HttpPost("group-cart/join")]
	public async Task<IActionResult> JoinGroupCart(string code)
	{
		var cart = await _context.SharedCarts.FirstOrDefaultAsync(c => c.JoinCode == code && c.IsActive);
		if (cart == null) return NotFound("Invalid code");

		var userId = GetUserId();
		if (!cart.ParticipantUserIds.Contains(userId))
		{
			cart.ParticipantUserIds.Add(userId);
			await _context.SaveChangesAsync();

			// Notify Group
			await _hubContext.Clients.Group(cart.Id.ToString()).SendAsync("UserJoined", userId);
		}

		return Ok(new { Message = "Joined Group Cart", CartId = cart.Id });
	}

	// NOTE: Adding items to group cart would basically replicate the standard Cart logic 
	// but target the SharedCart table and broadcast updates via SignalR.
}