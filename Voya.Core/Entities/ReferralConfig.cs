namespace Voya.Core.Entities;

public class ReferralConfig
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public decimal ReferrerReward { get; set; } = 5.0m; // Giver gets $5
	public decimal RefereeReward { get; set; } = 5.0m; // Receiver gets $5
	public bool IsActive { get; set; } = true;
	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}