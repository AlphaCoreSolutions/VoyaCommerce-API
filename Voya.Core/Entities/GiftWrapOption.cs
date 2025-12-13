namespace Voya.Core.Entities;

public class GiftWrapOption
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid StoreId { get; set; }

	public string Name { get; set; } = string.Empty; // e.g. "Gold Foil w/ Ribbon"
	public string Description { get; set; } = string.Empty;
	public decimal Price { get; set; } = 0; // Can be free
	public string? ImageUrl { get; set; }

	public bool IsActive { get; set; } = true;
}