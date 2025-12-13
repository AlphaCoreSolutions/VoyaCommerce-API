namespace Voya.Core.Entities;

public enum BlacklistStatus { Pending, Approved, Rejected }

public class BlacklistRequest
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid StoreId { get; set; }
	public Guid TargetUserId { get; set; } // The abusive customer

	public string Reason { get; set; } = string.Empty;
	public string EvidenceUrl { get; set; } = string.Empty; // Screenshot link

	public BlacklistStatus Status { get; set; } = BlacklistStatus.Pending;
	public string? AdminResponse { get; set; }
}