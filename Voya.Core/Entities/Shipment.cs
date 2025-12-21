using System.ComponentModel.DataAnnotations.Schema;
using Voya.Core.Enums; // Assuming you have this

namespace Voya.Core.Entities;

public class Shipment
{
	public Guid Id { get; set; } = Guid.NewGuid();

	// Link to Order (Parent)
	public Guid OrderId { get; set; }
	public Order Order { get; set; } = null!;

	// === NEW: Critical for Split Addresses ===
	// Each shipment can go to a different address.
	public Guid AddressId { get; set; }
	public Address Address { get; set; } = null!;

	// === NEW: Link Items to this Shipment ===
	// Which items are inside this specific box?
	public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

	// Link to Logistics Provider (3PL)
	public Guid? ProviderId { get; set; }

	// Link to Internal Driver
	public Guid? DriverId { get; set; }
	public DriverProfile? Driver { get; set; }

	// Tracking & Logistics
	public string TrackingNumber { get; set; } = string.Empty;
	public string ExternalLabelUrl { get; set; } = string.Empty;
	public decimal ShippingCost { get; set; } = 0m; // Cost for this specific box

	// Status Management
	public ShipmentStatus Status { get; set; } = ShipmentStatus.Pending;
	public string CurrentStatusRaw { get; set; } = string.Empty;

	public string CurrentLocation { get; set; } = "Warehouse";
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public DateTime EstimatedDeliveryTime { get; set; }
	public DateTime? ActualDeliveryTime { get; set; }
}