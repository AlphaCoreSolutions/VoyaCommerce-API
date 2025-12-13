namespace Voya.Core.Entities;

public class StockSubscription
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid ProductId { get; set; }
	public Guid UserId { get; set; } // Or Email for guests
	public string? Email { get; set; }
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public bool IsNotified { get; set; } = false;
}