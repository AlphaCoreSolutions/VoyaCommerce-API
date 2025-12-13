namespace Voya.Core.Entities;

// 1. ENUMS MUST BE OUTSIDE THE CLASS
public enum DeliveryTaskType { Pickup, Delivery, ReturnPickup }
public enum DeliveryTaskStatus { Assigned, Accepted, InProgress, Completed, Failed }
// ^ Renamed to 'DeliveryTaskStatus' to completely avoid conflict with System.Threading.Tasks.TaskStatus

public class DeliveryTask
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public Guid? DriverId { get; set; }
	public DriverProfile? Driver { get; set; }

	public Guid OrderId { get; set; }
	public Order Order { get; set; } = null!;

	// 2. USE THE UNIQUE ENUM NAME
	public DeliveryTaskStatus Status { get; set; } = DeliveryTaskStatus.Assigned;
	public DeliveryTaskType Type { get; set; } = DeliveryTaskType.Delivery;

	public string AddressText { get; set; } = string.Empty;
	public double DestinationLat { get; set; }
	public double DestinationLng { get; set; }

	public string? ProofPhotoUrl { get; set; }
	public string? CustomerSignatureUrl { get; set; }
	public DateTime? CompletedAt { get; set; }

	public int SequenceOrder { get; set; }
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}