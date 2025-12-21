using System.ComponentModel.DataAnnotations.Schema;

namespace Voya.Core.Entities;

public enum CampaignStatus { Active, Funded, Failed, FundsReleased, Refunded }

public class Campaign
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Title { get; set; } = string.Empty;

	// === FIX 1: Add Link to User (Creator) ===
	public Guid CreatorId { get; set; }
	public User Creator { get; set; } = null!;

	public string CreatorName { get; set; } = string.Empty; // Keep as snapshot

	public decimal GoalAmount { get; set; }
	public decimal CurrentAmount { get; set; }

	public DateTime EndDate { get; set; }
	public CampaignStatus Status { get; set; } = CampaignStatus.Active;
}