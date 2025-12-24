namespace Voya.Core.Entities;

public class AuctionReminder
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public Guid AuctionId { get; set; }
	public Auction Auction { get; set; } = null!;

	public Guid UserId { get; set; }
	public User User { get; set; } = null!;

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}