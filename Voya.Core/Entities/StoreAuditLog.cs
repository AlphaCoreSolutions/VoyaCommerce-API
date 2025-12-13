namespace Voya.Core.Entities;

public class StoreAuditLog
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid StoreId { get; set; }
	public Guid StaffUserId { get; set; } // Who did it?
	public string Action { get; set; } = string.Empty; // "Updated Product Price"
	public string Details { get; set; } = string.Empty; // "Changed iPhone from $999 to $899"
	public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}