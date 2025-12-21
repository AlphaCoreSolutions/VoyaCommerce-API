using System.ComponentModel.DataAnnotations.Schema;

namespace Voya.Core.Entities;

public enum CartType { Solo, Group }

public class Cart
{
	public Guid Id { get; set; } = Guid.NewGuid();

	// In Solo mode, this is the owner. In Group mode, this is the Manager.
	public Guid UserId { get; set; }

	public CartType Type { get; set; } = CartType.Solo;

	// Unique token for invite links (e.g., "AF7-B29")
	public string? SharingToken { get; set; }

	public ICollection<CartItem> Items { get; set; } = new List<CartItem>();

	// NEW: List of members in this cart
	public ICollection<CartMember> Members { get; set; } = new List<CartMember>();

	public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
	public bool IsActive { get; set; } = true; // Use this to archive old group carts
}

public class CartItem
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid CartId { get; set; }
	public Guid ProductId { get; set; }

	public int Quantity { get; set; }
	public string SelectedOptionsJson { get; set; } = "{}";

	// NEW: Track who added this item
	public Guid AddedByUserId { get; set; }

	// Optional: Snapshotted name so we don't need to join User table every time
	public string AddedByName { get; set; } = "Unknown";

	public Product Product { get; set; } = null!;
}