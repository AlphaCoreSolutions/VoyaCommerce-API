namespace Voya.Core.Entities;

public class CustomerTag
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid StoreId { get; set; }
	public Guid CustomerUserId { get; set; }

	public string Tag { get; set; } = string.Empty; // "VIP", "Big Spender"
	public DateTime TaggedAt { get; set; } = DateTime.UtcNow;
}