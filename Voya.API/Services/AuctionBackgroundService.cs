using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voya.API.Hubs;
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

	public async Task CheckExpiredAuctions()
	{
		try
		{
			var now = DateTime.UtcNow;

			// 1. Find Expired Active Auctions
			// Note: We removed .Include(a => a.Product) because Auction is now standalone
			var expiredAuctions = await _context.Auctions
				.Where(a => a.Status == AuctionStatus.Active && a.EndTime <= now)
				.ToListAsync();

			if (!expiredAuctions.Any()) return;

			foreach (var auction in expiredAuctions)
			{
				if (auction.CurrentWinnerId != null)
				{
					// === SCENARIO: AUCTION SOLD ===

					// A. Mark Auction as SOLD
					auction.Status = AuctionStatus.Sold;
					_logger.LogInformation($"Auction {auction.Id} SOLD to {auction.CurrentWinnerId} for {auction.CurrentHighestBid:C}");

					// B. Create the "Order" for the winner to pay
					// We treat the Auction itself as the "Product" for the sake of the OrderItem
					var order = new Order
					{
						Id = Guid.NewGuid(),
						UserId = auction.CurrentWinnerId.Value,
						PlacedAt = DateTime.UtcNow,
						Status = OrderStatus.Pending,       // Pending payment
						PaymentStatus = PaymentStatus.Unpaid,
						TotalAmount = auction.CurrentHighestBid,
						SubTotal = auction.CurrentHighestBid,
						// We can add a flag or type to distinguish this order source if needed
						// Source = OrderSource.Auction 
					};

					// C. Create the Order Item Snapshot
					var orderItem = new OrderItem
					{
						OrderId = order.Id,
						// CRITICAL: We link the Item back to the Auction ID 
						// effectively treating the Auction as the "Product" ID for lookup later
						ProductId = auction.Id,
						ProductName = auction.Title, // Use Auction Title
													 // Assuming your OrderItem has an ImageUrl field, or we rely on Product lookup
													 // If OrderItem doesn't have ImageUrl, you might rely on the ProductId lookup
						Quantity = 1,
						UnitPrice = auction.CurrentHighestBid
					};

					order.Items.Add(orderItem);
					_context.Orders.Add(order);

					// D. Notify Winner (Real-time)
					await _hubContext.Clients.User(auction.CurrentWinnerId.Value.ToString())
						.SendAsync("AuctionWon", new
						{
							AuctionId = auction.Id,
							Title = auction.Title, // Changed from Product.Name
							Price = auction.CurrentHighestBid
						});
				}
				else
				{
					// === SCENARIO: NO BIDS ===
					auction.Status = AuctionStatus.Ended;
					_logger.LogInformation($"Auction {auction.Id} ENDED without bids.");
				}
			}

			await _context.SaveChangesAsync();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error checking expired auctions");
			throw;
		}
	}
}