namespace Voya.Application.DTOs.Nexus;

public class NexusTaxRuleDto
{
	public Guid Id { get; set; }
	public string Name { get; set; } = "";
	public string CountryCode { get; set; } = "JO";
	public string? Region { get; set; }
	public string? City { get; set; }
	public Guid? CategoryId { get; set; }
	public Guid? StoreId { get; set; }
	public decimal RatePercent { get; set; }
	public bool IsActive { get; set; }
	public int Priority { get; set; }
}

public class NexusUpsertTaxRuleRequest
{
	public string Name { get; set; } = "";
	public string CountryCode { get; set; } = "JO";
	public string? Region { get; set; }
	public string? City { get; set; }
	public Guid? CategoryId { get; set; }
	public Guid? StoreId { get; set; }
	public decimal RatePercent { get; set; }
	public bool IsActive { get; set; } = true;
	public int Priority { get; set; } = 100;
}

public class NexusTaxSettingsDto
{
	public bool TaxEnabled { get; set; }
	public decimal DefaultRatePercent { get; set; }
	public bool PricesIncludeTax { get; set; }
}

public class NexusUpdateTaxSettingsRequest
{
	public bool TaxEnabled { get; set; }
	public decimal DefaultRatePercent { get; set; }
	public bool PricesIncludeTax { get; set; }
}

public class NexusTaxPreviewRequest
{
	public decimal SubTotal { get; set; }
	public string CountryCode { get; set; } = "JO";
	public string? Region { get; set; }
	public string? City { get; set; }
	public Guid? CategoryId { get; set; }
	public Guid? StoreId { get; set; }
}

public class NexusTaxPreviewResponse
{
	public decimal RatePercent { get; set; }
	public decimal TaxAmount { get; set; }
	public decimal TotalWithTax { get; set; }
	public string AppliedRuleName { get; set; } = "";
}
