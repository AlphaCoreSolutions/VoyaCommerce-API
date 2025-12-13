namespace Voya.Core.Entities;

public enum ProviderType { Internal, ExternalApi }

public class LogisticsProvider
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Name { get; set; } = string.Empty; // "Voya Fleet", "Aramex", "DHL"
	public ProviderType Type { get; set; }
	public bool IsActive { get; set; } = false; // Only one should be active at a time

	// Configuration for External APIs (Stored securely)
	public string ApiBaseUrl { get; set; } = string.Empty;
	public string ApiKey { get; set; } = string.Empty;
	public string ApiSecret { get; set; } = string.Empty;
	public string WebhookSecret { get; set; } = string.Empty;

	// Feature Flags
	public bool SupportsLabelGeneration { get; set; } = true;
	public bool SupportsRealTimeTracking { get; set; } = false;
}