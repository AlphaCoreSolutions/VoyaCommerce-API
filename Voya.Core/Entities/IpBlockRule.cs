namespace Voya.Core.Entities;

public class IpBlockRule
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string IpAddress { get; set; } = string.Empty; // Single IP or CIDR
	public string Reason { get; set; } = string.Empty;
	public Guid BlockedByAdminId { get; set; }
	public DateTime BlockedAt { get; set; } = DateTime.UtcNow;
}