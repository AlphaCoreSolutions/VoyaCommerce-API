
using Voya.Core.Entities;
using Voya.Core.Enums;

public class Product
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid StoreId { get; set; } // Multi-vendor support
	public Guid CategoryId { get; set; }

	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;

	// --- NEW: Search Tags ---
	// Stores keywords like ["sneaker", "running", "red", "nike"]
	public List<string> Tags { get; set; } = new();

	public decimal BasePrice { get; set; }
	public decimal? DiscountPrice { get; set; }
	public int StockQuantity { get; set; }

	public string MainImageUrl { get; set; } = string.Empty;
	public List<string> GalleryImages { get; set; } = new();

	// Storing complex options (Size/Color) as JSONB in Postgres is often 
	// better for performance, but we will use a separate entity for strict schema.
	public ICollection<ProductOption> Options { get; set; } = new List<ProductOption>();

	public Category Category { get; set; } = null!;

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	// 1. Soft Delete / Hide from Store
	public bool IsActive { get; set; } = true;

	// 2. Multi-Warehouse Inventory
	public ICollection<ProductInventory> Inventories { get; set; } = new List<ProductInventory>();

	// 3. B2B Wholesale Pricing
	public ICollection<ProductTierPrice> TierPrices { get; set; } = new List<ProductTierPrice>();

	// 4. Subscriptions
	public ICollection<SubscriptionPlan> SubscriptionPlans { get; set; } = new List<SubscriptionPlan>();

	public bool IsPreOrder { get; set; } = false;
	public DateTime? PreOrderAvailableDate { get; set; }
	public ProductApprovalStatus ApprovalStatus { get; set; } = ProductApprovalStatus.PendingReview;
	public string? RejectionReason { get; set; }

}

public class ProductOption
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid ProductId { get; set; }
	public string Name { get; set; } = string.Empty; // e.g., "Size", "Color"
	public List<ProductOptionValue> Values { get; set; } = new();
}

public class ProductOptionValue
{
	public string Id { get; set; } = string.Empty; // e.g., "xl", "red"
	public string Label { get; set; } = string.Empty; // e.g., "Extra Large"
	public decimal PriceModifier { get; set; } = 0;
}