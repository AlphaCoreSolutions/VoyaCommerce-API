namespace Voya.Core.Entities;

public enum DocumentType { TermsOfService, PrivacyPolicy, SellerAgreement }

public class LegalDocument
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public DocumentType Type { get; set; }
	public string Version { get; set; } = "1.0.0";
	public string ContentMarkdown { get; set; } = string.Empty;
	public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
	public bool ForceReacceptance { get; set; } = false;
}