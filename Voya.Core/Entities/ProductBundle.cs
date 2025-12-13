namespace Voya.Core.Entities;

public class ProductBundle
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid StoreId { get; set; }

	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public decimal TotalPrice { get; set; } // The special bundle price

	// Which products are in this bundle?
	public List<Guid> ProductIds { get; set; } = new();

	public bool IsActive { get; set; } = true;
}