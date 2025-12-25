namespace Voya.Application.DTOs.Nexus;

public class NexusPagedResult<T>
{
	public List<T> Items { get; set; } = new();
	public int Page { get; set; }
	public int PageSize { get; set; }
	public int TotalCount { get; set; }
}

public class NexusOrderListItemDto
{
	public Guid Id { get; set; }
	public string Status { get; set; } = string.Empty;
	public string PaymentStatus { get; set; } = string.Empty;

	public decimal TotalAmount { get; set; }
	public DateTime PlacedAt { get; set; }

	public Guid UserId { get; set; }
	public string UserName { get; set; } = string.Empty;
	public string UserEmail { get; set; } = string.Empty;

	public string? TrackingNumber { get; set; }
	public string? Carrier { get; set; }
}

public class NexusOrderDetailDto
{
	public Guid Id { get; set; }
	public string Status { get; set; } = string.Empty;
	public string PaymentStatus { get; set; } = string.Empty;

	public decimal SubTotal { get; set; }
	public decimal VoucherDiscount { get; set; }
	public decimal PointsDiscount { get; set; }
	public decimal TotalAmount { get; set; }
	public DateTime PlacedAt { get; set; }

	public Guid UserId { get; set; }
	public string UserName { get; set; } = string.Empty;
	public string UserEmail { get; set; } = string.Empty;
	public string? UserPhone { get; set; }

	// Snapshots (already exist in entity)
	public string ShippingAddressJson { get; set; } = string.Empty;
	public string PaymentMethodJson { get; set; } = string.Empty;

	public string? TrackingNumber { get; set; }
	public string? Carrier { get; set; }

	public List<NexusOrderItemDto> Items { get; set; } = new();
	public List<NexusShipmentDto> Shipments { get; set; } = new();
}

public class NexusOrderItemDto
{
	public Guid Id { get; set; }
	public Guid ProductId { get; set; }
	public string ProductName { get; set; } = string.Empty;
	public int Quantity { get; set; }
	public decimal UnitPrice { get; set; }
	public Guid? ShipmentId { get; set; }
}

public class NexusShipmentDto
{
	public Guid Id { get; set; }
	public string Status { get; set; } = string.Empty;

	public string TrackingNumber { get; set; } = string.Empty;
	public string ExternalLabelUrl { get; set; } = string.Empty;
	public decimal ShippingCost { get; set; }

	public Guid AddressId { get; set; }
	public string AddressSummary { get; set; } = string.Empty;

	public string CurrentLocation { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; }
	public DateTime EstimatedDeliveryTime { get; set; }
	public DateTime? ActualDeliveryTime { get; set; }
}

