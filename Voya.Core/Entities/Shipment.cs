using System.ComponentModel.DataAnnotations.Schema;
using Voya.Core.Enums;

namespace Voya.Core.Entities;


public class Shipment
{
	public Guid Id { get; set; } = Guid.NewGuid();

	// Link to Order
	public Guid OrderId { get; set; }
	public Order Order { get; set; } = null!;

	// Link to Logistics Provider (Legacy/3PL)
	public Guid? ProviderId { get; set; }

	// Link to Internal Driver (NEW - Required for Driver App)
	public Guid? DriverId { get; set; }
	public DriverProfile? Driver { get; set; }

	// Tracking
	public string TrackingNumber { get; set; } = string.Empty; // Renamed from ExternalTrackingNumber for consistency
	public string ExternalLabelUrl { get; set; } = string.Empty;

	// Status Management
	public ShipmentStatus Status { get; set; } = ShipmentStatus.Pending; // FIX: Added Status Enum Property
	public string CurrentStatusRaw { get; set; } = string.Empty; // Keep for legacy string codes if needed

	// Logistics Data
	public string CurrentLocation { get; set; } = "Warehouse";
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	// Delivery Times
	public DateTime EstimatedDeliveryTime { get; set; } // Renamed from EstimatedDelivery
	public DateTime? ActualDeliveryTime { get; set; } // FIX: Added for completion logic
}