// Links a Product to a Warehouse with a specific count
public class ProductInventory
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid ProductId { get; set; }
	public Guid WarehouseId { get; set; }

	public int Quantity { get; set; } = 0;
	public string? ShelfLocation { get; set; } // "Row A, Bin 2"
}