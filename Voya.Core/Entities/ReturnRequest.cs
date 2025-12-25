using Voya.Core.Enums;

namespace Voya.Core.Entities;

public enum ReturnStatus
{
	Pending = 0,
	UnderInspection = 1,
	Approved = 2,
	Rejected = 3,
	Completed = 4
}
public enum ReturnReason { Damaged, WrongItem, DidNotFit, ChangedMind }


public class ReturnRequest
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public Guid OrderId { get; set; }
	public Order Order { get; set; } = null!;

	public Guid UserId { get; set; }
	public User User { get; set; } = null!;

	// optional: per-item return (if your system supports it)
	public Guid? OrderItemId { get; set; }
	public OrderItem? OrderItem { get; set; }

	public ReturnStatus Status { get; set; } = ReturnStatus.Pending;

	public string Reason { get; set; } = string.Empty;
	public string EvidenceUrlsJson { get; set; } = "[]"; // images/videos array

	// inspection fields
	public string InspectionNote { get; set; } = string.Empty;
	public bool Restock { get; set; }
	public string? RejectionReason { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
