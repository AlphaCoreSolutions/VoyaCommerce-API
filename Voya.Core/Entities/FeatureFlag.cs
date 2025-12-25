namespace Voya.Core.Entities;

public class FeatureFlag
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Key { get; set; } = string.Empty; // e.g. "EnableCashOnDelivery"
	public bool IsEnabled { get; set; } = false;
	public string Description { get; set; } = string.Empty;
	public DateTime UpdatedAt { get; set; }
}