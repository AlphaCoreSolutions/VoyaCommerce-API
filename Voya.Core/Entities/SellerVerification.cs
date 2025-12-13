namespace Voya.Core.Entities;

public enum VerificationStatus { Pending, Approved, Rejected }

public class SellerVerification
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid StoreId { get; set; }
	public string DocumentUrl { get; set; } = string.Empty; // ID/License Image
	public string DocumentType { get; set; } = "BusinessLicense";
	public VerificationStatus Status { get; set; } = VerificationStatus.Pending;
	public string? AdminNote { get; set; }
}