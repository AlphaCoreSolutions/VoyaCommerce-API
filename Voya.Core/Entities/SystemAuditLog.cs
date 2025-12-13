namespace Voya.Core.Entities;

public class SystemAuditLog
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid AdminUserId { get; set; }
	public string Action { get; set; } = string.Empty; // "Banned User", "Changed Fee"
	public string EntityId { get; set; } = string.Empty; // The ID of the thing changed
	public string OldValue { get; set; } = string.Empty; // Snapshot before
	public string NewValue { get; set; } = string.Empty; // Snapshot after
	public string IpAddress { get; set; } = string.Empty;
	public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}