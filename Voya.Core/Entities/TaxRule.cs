namespace Voya.Core.Entities;

public class TaxRule
{
	public Guid Id { get; set; } = Guid.NewGuid();

	// Scope
	public string CountryCode { get; set; } = "JO";     // ISO-2
	public string? Region { get; set; }                 // Optional
	public string? City { get; set; }                   // Optional

	// Optional targeting
	public Guid? CategoryId { get; set; }               // apply to category
	public Guid? StoreId { get; set; }                  // apply to a store/vendor

	// Rate
	public decimal RatePercent { get; set; }            // e.g. 16 = 16%

	// Control
	public bool IsActive { get; set; } = true;
	public int Priority { get; set; } = 100;            // lower wins if multiple match

	// Metadata
	public string Name { get; set; } = "Default Tax Rule";
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
