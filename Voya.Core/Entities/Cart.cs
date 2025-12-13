namespace Voya.Core.Entities;

public class Cart
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid UserId { get; set; }

	// Navigation
	public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
	public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class CartItem
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid CartId { get; set; }
	public Guid ProductId { get; set; }

	public int Quantity { get; set; }

	// Store specific choices: e.g., "Size: XL"
	public string SelectedOptionsJson { get; set; } = "{}";

	public Product Product { get; set; } = null!;
}