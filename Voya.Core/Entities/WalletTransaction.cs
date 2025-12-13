namespace Voya.Core.Entities;

public enum TransactionType { Sale, Refund, Payout, Fee }

public class WalletTransaction
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid StoreId { get; set; }

	public decimal Amount { get; set; } // Positive for Sales, Negative for Refunds/Payouts
	public TransactionType Type { get; set; }
	public string Description { get; set; } = string.Empty; // e.g., "Order #12345"

	public DateTime Date { get; set; } = DateTime.UtcNow;
}

public class PayoutRequest
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid StoreId { get; set; }
	public decimal Amount { get; set; }
	public string BankDetailsJson { get; set; } = string.Empty; // IBAN, Swift, etc.
	public bool IsProcessed { get; set; } = false;
	public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}