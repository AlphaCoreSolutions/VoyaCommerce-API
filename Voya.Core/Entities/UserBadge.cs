namespace Voya.Core.Entities;

public class UserBadge
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid UserId { get; set; }
	public string BadgeName { get; set; } = "Top Reviewer";
	public string IconUrl { get; set; } = string.Empty;
	public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
}