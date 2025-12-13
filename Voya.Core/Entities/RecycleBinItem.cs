namespace Voya.Core.Entities;

public class RecycleBinItem
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string EntityType { get; set; } = string.Empty; // "Product", "Store"
	public string OriginalId { get; set; } = string.Empty;
	public string JsonData { get; set; } = string.Empty; // Serialized backup
	public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
	public Guid DeletedByUserId { get; set; }
}