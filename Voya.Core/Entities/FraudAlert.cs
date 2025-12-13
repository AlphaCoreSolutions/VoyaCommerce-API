namespace Voya.Core.Entities;

public enum FraudRiskLevel { Low, Medium, High, Critical }

public class FraudAlert
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid OrderId { get; set; }
	public Guid UserId { get; set; }

	public double RiskScore { get; set; } // 0 to 100
	public FraudRiskLevel Level { get; set; }
	public string Reason { get; set; } = string.Empty; // "IP Mismatch", "High Velocity"

	public bool IsResolved { get; set; } = false;
	public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}