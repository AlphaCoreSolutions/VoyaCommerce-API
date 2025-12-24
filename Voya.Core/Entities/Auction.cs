using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voya.Core.Entities;

public enum AuctionStatus
{
	Draft,              // User is still editing
	PendingApproval,    // Submitted, waiting for Admin (NEW)
	Upcoming,           // Approved, waiting for StartTime (NEW)
	Active,             // Live bidding
	Ended,              // Time up, processing winner
	Sold,               // Winner paid
	Shipped,            // Seller sent item
	Delivered,          // Buyer received
	Cancelled,
	Rejected            // Admin rejected
}

public class Auction
{
	public Guid Id { get; set; } = Guid.NewGuid();

	// --- NEW: Standalone Listing Data (Replaces ProductId) ---
	public string Title { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public string MainImageUrl { get; set; } = string.Empty;

	// Storing list of images as a simple property (handled as JSON or separate table usually)
	// For simplicity in EF Core, we often skip mapping this or use a value converter.
	// To keep it simple for now, we will ignore it in EF or use a List<string> wrapper.
	[NotMapped]
	public List<string> ImageGallery { get; set; } = new List<string>();

	// Who is selling it?
	public Guid SellerId { get; set; }
	public User Seller { get; set; } = null!;

	// Financials
	public decimal StartPrice { get; set; }
	public decimal ReservePrice { get; set; } = 0;

	[ConcurrencyCheck]
	public decimal CurrentHighestBid { get; set; } = 0;

	// Winner Info
	public Guid? CurrentWinnerId { get; set; }
	public User? CurrentWinner { get; set; }

	// Timing
	public DateTime StartTime { get; set; }
	public DateTime EndTime { get; set; }

	public AuctionStatus Status { get; set; } = AuctionStatus.Draft;

	// Navigation
	public ICollection<AuctionBid> Bids { get; set; } = new List<AuctionBid>();

	// === NEW: Reminders Relationship ===
	public ICollection<AuctionReminder> Reminders { get; set; } = new List<AuctionReminder>();
}