namespace Voya.Core.Entities;

public enum CampaignStatus { Active, Funded, Failed, FundsReleased, Refunded }

public class Campaign
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Title { get; set; } = string.Empty;
	public string CreatorName { get; set; } = string.Empty;

	public decimal GoalAmount { get; set; }
	public decimal CurrentAmount { get; set; }

	public DateTime EndDate { get; set; }
	public CampaignStatus Status { get; set; } = CampaignStatus.Active;

	// In a real app, you'd have a list of Backers here
	// public List<Backer> Backers { get; set; } 
}