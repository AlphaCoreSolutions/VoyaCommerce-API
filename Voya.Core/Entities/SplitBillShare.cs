namespace Voya.Core.Entities;

public enum ShareStatus { Pending, Paid, Failed }

public class SplitBillShare
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public Guid SplitBillId { get; set; }
	public SplitBill SplitBill { get; set; } = null!;

	public Guid UserId { get; set; }
	public User User { get; set; } = null!;

	public decimal AmountDue { get; set; }
	public decimal AmountPaid { get; set; } = 0;

	public ShareStatus Status { get; set; } = ShareStatus.Pending;
	public DateTime? PaidAt { get; set; }

	// Optional: Store the PaymentIntentId if using Stripe
	public string? PaymentReference { get; set; }
}