namespace Voya.Core.Entities;

public class Category
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Name { get; set; } = string.Empty;

	// Visuals
	public string? IconUrl { get; set; } // For remote images (S3/Cloudinary)
	public string? ColorHex { get; set; } // For UI styling

	// --- NEW FIELDS FOR NEXUS ---
	public string Icon { get; set; } = "inventory"; // For local Flutter Material Icons (e.g. "inventory")
	public bool IsActive { get; set; } = true;      // To hide/show in app
	public int DisplayOrder { get; set; } = 0;      // For sorting

	// Recursive Relationship
	public Guid? ParentId { get; set; }
	public Category? Parent { get; set; }
	public ICollection<Category> SubCategories { get; set; } = new List<Category>();

	// Navigation
	public ICollection<Product> Products { get; set; } = new List<Product>();
}