namespace Voya.Core.Entities;

public class GlobalSetting
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Key { get; set; } = string.Empty; // e.g., "PlatformFeePercent"
	public string Value { get; set; } = string.Empty; // e.g., "5.0"
	public string Description { get; set; } = string.Empty;
	public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}