namespace Voya.Core.Entities;

public class StoreStrike
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid StoreId { get; set; }
	public string Reason { get; set; } = string.Empty;
	public Guid IssuedByAdminId { get; set; }
	public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
}