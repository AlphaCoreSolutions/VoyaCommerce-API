namespace Voya.Core.Entities;

public class TaxSettings
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public bool TaxEnabled { get; set; } = true;

	// If no rule matches, this is used
	public decimal DefaultRatePercent { get; set; } = 0;

	// Future toggles
	public bool PricesIncludeTax { get; set; } = false;

	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
