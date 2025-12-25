namespace Voya.Core.Entities;

public class AiFeatureFlag
{
	public Guid Id { get; set; } = Guid.NewGuid();

	// key like: "inventory_predictions", "fraud_risk_scoring"
	public string Key { get; set; } = string.Empty;

	public bool IsEnabled { get; set; }

	// Optional config: JSON string to store thresholds etc.
	public string ConfigJson { get; set; } = "{}";

	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

	public Guid? UpdatedByUserId { get; set; }
}
