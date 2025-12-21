namespace Voya.Core.Entities;

public enum TransactionType
{
	Sale,
	Refund,
	Payout,
	Fee,
	AuctionSale
}

// === FIX 2: Add TransactionStatus Enum ===
public enum TransactionStatus
{
	Pending,
	Completed,
	Failed,
	Cancelled
}

public class WalletTransaction
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public Guid? StoreId { get; set; }
	public Guid? UserId { get; set; }

	public decimal Amount { get; set; }
	public TransactionType Type { get; set; }

	// === FIX 3: Add Status Property ===
	public TransactionStatus Status { get; set; } = TransactionStatus.Completed;

	public string Description { get; set; } = string.Empty;

	public DateTime Date { get; set; } = DateTime.UtcNow;

	public Guid? OrderId { get; set; }
}
public class PayoutRequest
{
	public Guid Id { get; set; } = Guid.NewGuid();

	// === CHANGE: Support User Payouts ===
	public Guid? StoreId { get; set; }
	public Guid? UserId { get; set; }

	public decimal Amount { get; set; }
	public string BankDetailsJson { get; set; } = string.Empty;
	public bool IsProcessed { get; set; } = false;
	public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}