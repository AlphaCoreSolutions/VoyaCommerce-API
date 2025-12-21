using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voya.Core.Entities;

public enum AuctionStatus { Draft, Active, Ended, Sold, Cancelled }

public class Auction
{
	public Guid Id { get; set; } = Guid.NewGuid();

	// The Product being sold
	public Guid ProductId { get; set; }
	public Product Product { get; set; } = null!;

	// Who is selling it? (Admin or User)
	public Guid SellerId { get; set; }
	public User Seller { get; set; } = null!;

	// Financials
	public decimal StartPrice { get; set; }
	public decimal ReservePrice { get; set; } = 0; // Minimum price to sell

	[ConcurrencyCheck] // Helps prevent race conditions
	public decimal CurrentHighestBid { get; set; } = 0;

	// Who is currently winning?
	public Guid? CurrentWinnerId { get; set; }
	public User? CurrentWinner { get; set; }

	// Timing
	public DateTime StartTime { get; set; }
	public DateTime EndTime { get; set; }

	public AuctionStatus Status { get; set; } = AuctionStatus.Draft;

	// Navigation
	public ICollection<AuctionBid> Bids { get; set; } = new List<AuctionBid>();
}