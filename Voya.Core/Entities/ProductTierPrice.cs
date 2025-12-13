namespace Voya.Core.Entities;

public class ProductTierPrice
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid ProductId { get; set; }

	public int MinQuantity { get; set; } // e.g., 50
	public decimal UnitPrice { get; set; } // e.g., $5.00
}