
using Voya.Core.Entities;
using Voya.Core.Enums;

using Voya.Core.Entities;
using Voya.Core.Enums;
using System.ComponentModel.DataAnnotations.Schema; // Add this

public class Product
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public Guid StoreId { get; set; }
	// 1. ADD THIS NAVIGATION PROPERTY
	[ForeignKey("StoreId")]
	public virtual Store? Store { get; set; }

	public Guid CategoryId { get; set; }
	public Category Category { get; set; } = null!;

	// ... (Keep the rest of your properties: Name, Description, Tags, etc.)
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public List<string> Tags { get; set; } = new();
	public decimal BasePrice { get; set; }
	public decimal? DiscountPrice { get; set; }
	public int StockQuantity { get; set; }
	public string MainImageUrl { get; set; } = string.Empty;
	public List<string> GalleryImages { get; set; } = new();
	public ICollection<ProductOption> Options { get; set; } = new List<ProductOption>();
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public bool IsActive { get; set; } = true;
	public ICollection<ProductInventory> Inventories { get; set; } = new List<ProductInventory>();
	public ICollection<ProductTierPrice> TierPrices { get; set; } = new List<ProductTierPrice>();
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