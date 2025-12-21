using System.ComponentModel.DataAnnotations.Schema;

namespace Voya.Core.Entities;

public enum OrderStatus
{
	Pending,
	Processing,
	Shipped,
	Delivered,
	Cancelled,
	ReturnRequested,
	Refunded,
	Returned
}

public enum PaymentStatus { Unpaid, Paid, Refunded }
public enum PaymentType { CreditCard, CashOnDelivery }

public class Order
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public Guid UserId { get; set; }
	public User User { get; set; } = null!;

	// Links multiple orders together if split from one cart
	public string? GroupTransactionId { get; set; }

	// Nullable because in split shipments, addresses are defined per Shipment
	public Guid? ShippingAddressId { get; set; }

	public PaymentType PaymentType { get; set; } = PaymentType.CreditCard;
	public Guid? PaymentMethodId { get; set; }

	// Snapshots
	public string ShippingAddressJson { get; set; } = string.Empty;
	public string PaymentMethodJson { get; set; } = string.Empty;

	public OrderStatus Status { get; set; } = OrderStatus.Pending;
	public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

	// Financials
	public decimal SubTotal { get; set; }
	public decimal VoucherDiscount { get; set; }
	public decimal PointsDiscount { get; set; }
	public int PointsRedeemed { get; set; }
	public decimal TotalAmount { get; set; }

	// Legacy Tracking (For single shipments). For splits, check Shipments collection.
	public string? TrackingNumber { get; set; }
	public string? Carrier { get; set; }

	public DateTime PlacedAt { get; set; } = DateTime.UtcNow;

	// Gift Wrap Data
	public bool IsGift { get; set; } = false;
	public string? GiftMessage { get; set; }
	public Guid? GiftWrapOptionId { get; set; }
	public string? GiftWrapName { get; set; }
	public decimal GiftWrapPrice { get; set; }

	// === NEW: Multi-Shipment Logic ===
	// An order is composed of 1 or more shipments (boxes)
	public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();

	// This remains to easily access all items without traversing shipments
	public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public class OrderItem
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public Guid OrderId { get; set; }
	public Order Order { get; set; } = null!;

	// === NEW: Link to specific Shipment (Box) ===
	public Guid? ShipmentId { get; set; }
	public Shipment? Shipment { get; set; }

	public Guid ProductId { get; set; }
	public Product? Product { get; set; }

	// Snapshot Data
	public string ProductName { get; set; } = string.Empty;
	public string MainImage { get; set; } = string.Empty; // Added based on controller needs
	public decimal UnitPrice { get; set; }
	public int Quantity { get; set; }
	public string? SelectedOptionsJson { get; set; }

	public decimal LineTotal => UnitPrice * Quantity;
}