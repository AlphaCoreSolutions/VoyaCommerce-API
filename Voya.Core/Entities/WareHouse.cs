namespace Voya.Core.Entities;

public class Warehouse
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid StoreId { get; set; }

	public string Name { get; set; } = string.Empty; // "Amman HQ"
	public string Address { get; set; } = string.Empty;
	public string City { get; set; } = string.Empty;
	public bool IsPrimary { get; set; } = false; // Default shipping location
}

