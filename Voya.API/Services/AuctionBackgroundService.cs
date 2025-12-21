using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voya.API.Hubs; // For SignalR notifications
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.Infrastructure.Services;

public interface IAuctionBackgroundService
{
	Task CheckExpiredAuctions();
}

public class AuctionBackgroundService : IAuctionBackgroundService
{
	private readonly VoyaDbContext _context;
	private readonly IHubContext<LiveHub> _hubContext;
	private readonly ILogger<AuctionBackgroundService> _logger;

	public AuctionBackgroundService(
		VoyaDbContext context,
		IHubContext<LiveHub> hubContext,
		ILogger<AuctionBackgroundService> logger)
	{
		_context = context;
		_hubContext = hubContext;
		_logger = logger;
	}

	// This method is called automatically by Hangfire every minute
	public async Task CheckExpiredAuctions()
	{
		try
		{
			var now = DateTime.UtcNow;

			// Find Active auctions that have expired
			var expiredAuctions = await _context.Auctions
				.Include(a => a.Product)
				.Where(a => a.Status == AuctionStatus.Active && a.EndTime <= now)
				.ToListAsync();

			if (!expiredAuctions.Any()) return;

			foreach (var auction in expiredAuctions)
			{
				if (auction.CurrentWinnerId != null)
				{
					// 1. Mark as SOLD
					auction.Status = AuctionStatus.Sold;
					_logger.LogInformation($"Auction {auction.Id} SOLD to {auction.CurrentWinnerId} for ${auction.CurrentHighestBid}");

					// 2. Create a "Pending" Order for the winner to pay
					// Note: In a full app, this logic might live in OrderService, 
					// but we do it here for transactional safety.
					var order = new Order
					{
						Id = Guid.NewGuid(),
						UserId = auction.CurrentWinnerId.Value,
						PlacedAt = DateTime.UtcNow,
						Status = OrderStatus.Pending,
						PaymentStatus = PaymentStatus.Unpaid, // Winner needs to go to "My Orders" and Pay
						TotalAmount = auction.CurrentHighestBid,
						SubTotal = auction.CurrentHighestBid,
						// Link specific auction product item...
						// (You would add OrderItem creation logic here)
					};
					_context.Orders.Add(order);

					// 3. Notify Winner (Real-time)
					await _hubContext.Clients.User(auction.CurrentWinnerId.Value.ToString())
						.SendAsync("AuctionWon", new
						{
							AuctionId = auction.Id,
							ProductName = auction.Product.Name,
							Price = auction.CurrentHighestBid
						});
				}
				else
				{
					// No bids? End it.
					auction.Status = AuctionStatus.Ended;
					_logger.LogInformation($"Auction {auction.Id} ENDED without bids.");
				}
			}

			await _context.SaveChangesAsync();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error checking expired auctions");
			throw; // Hangfire will retry automatically if we throw
		}
	}
}