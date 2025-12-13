namespace Voya.Core.Entities;

public enum ReturnStatus { Pending, Approved, Rejected, Refunded }
public enum ReturnReason { Damaged, WrongItem, DidNotFit, ChangedMind }

public class ReturnRequest
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid OrderId { get; set; }
	public Guid ProductId { get; set; } // Specific item being returned
	public Guid UserId { get; set; }    // Customer
	public Guid StoreId { get; set; }   // Seller

	public ReturnReason Reason { get; set; }
	public string? Comment { get; set; }
	public List<string> ProofImages { get; set; } = new();

	public ReturnStatus Status { get; set; } = ReturnStatus.Pending;
	public string? AdminNote { get; set; } // Seller's response

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}