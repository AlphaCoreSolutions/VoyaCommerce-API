namespace Voya.Core.Entities;

public class SensitiveAccessLog
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid AdminUserId { get; set; }
	public string Reason { get; set; } = string.Empty; // "Investigating Fraud"
	public string ResourceAccessed { get; set; } = string.Empty; // "User 1234 IBAN"
	public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}