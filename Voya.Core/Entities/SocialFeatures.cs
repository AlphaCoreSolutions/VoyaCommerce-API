namespace Voya.Core.Entities;

public class Review
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid UserId { get; set; }
	public Guid ProductId { get; set; }
	public Guid OrderId { get; set; } // Matches Order.Id // Verify they bought it

	public int Rating { get; set; } // 1 to 5
	public string? Comment { get; set; }
	public List<string> ImageUrls { get; set; } = new(); // User photos

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	// Navigation
	public User User { get; set; } = null!;
	public Product Product { get; set; } = null!;

	public string? Reply { get; set; }        // The store's response
	public DateTime? RepliedAt { get; set; }
}

public class WishlistItem
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid UserId { get; set; }
	public Guid ProductId { get; set; }
	public DateTime AddedAt { get; set; } = DateTime.UtcNow;

	public Product Product { get; set; } = null!;
}

public class Notification
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid UserId { get; set; }

	public string Title { get; set; } = string.Empty;
	public string Body { get; set; } = string.Empty;
	public string Type { get; set; } = "General"; // OrderUpdate, Promo, ReviewPrompt
	public string? RelatedEntityId { get; set; }  // OrderID or ProductID

	public bool IsRead { get; set; } = false;
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}