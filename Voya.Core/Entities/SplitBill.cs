using System.ComponentModel.DataAnnotations.Schema;

namespace Voya.Core.Entities;

public enum SplitBillStatus
{
	Pending,        // Just created, waiting for payments
	Collecting,     // Payments are coming in
	FullyPaid,      // Everyone paid, ready to convert to Order
	Completed,      // Order placed successfully
	Cancelled,      // Manager cancelled
	Expired         // Timed out (e.g. 24h limit)
}

public class SplitBill
{
	public Guid Id { get; set; } = Guid.NewGuid();

	// Link to the Group Cart
	public Guid CartId { get; set; }
	public Cart Cart { get; set; } = null!;

	// Who started this? (Usually the Cart Manager)
	public Guid InitiatorUserId { get; set; }

	public decimal TotalAmount { get; set; }
	public SplitBillStatus Status { get; set; } = SplitBillStatus.Pending;

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);

	// Navigation
	public ICollection<SplitBillShare> Shares { get; set; } = new List<SplitBillShare>();
}