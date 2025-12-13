namespace Voya.Core.Entities;

public enum SubscriptionInterval { Weekly, Monthly, Quarterly, Yearly }

public class SubscriptionPlan
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid ProductId { get; set; } // The product being subscribed to

	public string Name { get; set; } = string.Empty; // "Weekly Coffee"
	public decimal PricePerBilling { get; set; }
	public SubscriptionInterval Interval { get; set; }

	public bool IsActive { get; set; } = true;
}