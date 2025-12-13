namespace Voya.Core.Entities;

public class GameHistory
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid UserId { get; set; }

	public string GameName { get; set; } = string.Empty; // e.g. "SpinWheel", "MemoryMatch"
	public int PointsEarned { get; set; }
	public DateTime PlayedAt { get; set; } = DateTime.UtcNow;

	// Navigation (Optional, useful for Analytics)
	public User User { get; set; } = null!;
}