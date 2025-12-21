namespace Voya.Core.Entities;

public class AuctionBid
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public Guid AuctionId { get; set; }
	public Auction Auction { get; set; } = null!;

	public Guid UserId { get; set; }
	public User User { get; set; } = null!;

	public decimal Amount { get; set; }
	public DateTime PlacedAt { get; set; } = DateTime.UtcNow;
}