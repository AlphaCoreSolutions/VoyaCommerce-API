
namespace Voya.Application.DTOs;

// --- 1. Base / Create DTO ---
public class CreateProductDto
{
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;

	public decimal BasePrice { get; set; }
	public decimal? DiscountPrice { get; set; }

	public int StockQuantity { get; set; }

	public Guid CategoryId { get; set; }

	// Images
	public string MainImageUrl { get; set; } = string.Empty;
	public List<string> GalleryImages { get; set; } = new();

	public List<string> Tags { get; set; } = new();

	public List<CreateOptionDto> Options { get; set; } = new();
}

// --- 2. Update DTO ---
public class UpdateProductDto : CreateProductDto
{
	// Inherits all fields from CreateProductDto (Name, Price, Options, etc.)

	// START: Extra fields specific to Updates

	// Allows seller to hide a product without deleting it
	public bool IsActive { get; set; } = true;

	// END: Extra fields
}

// --- 3. Helper DTOs for Options ---
public class CreateOptionDto
{
	public string Name { get; set; } = string.Empty; // e.g. "Size", "Color"
	public List<CreateOptionValueDto> Values { get; set; } = new();
}

public class CreateOptionValueDto
{
	public string Label { get; set; } = string.Empty; // e.g. "XL", "Red"
	public decimal PriceModifier { get; set; } = 0;   // e.g. +$2.00
}