namespace Voya.Core.Entities;

public enum DriverStatus { Offline, Online, Busy }
public enum TaskStatus { Assigned, Accepted, InProgress, Completed, Failed }
public enum TaskType { Pickup, Delivery, ReturnPickup }

public class DriverProfile
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid UserId { get; set; } // Link to Login
	public User User { get; set; } = null!;
	public string VehiclePlate { get; set; } = string.Empty;
	public DriverStatus Status { get; set; } = DriverStatus.Offline;
	public double CurrentLat { get; set; }
	public double CurrentLng { get; set; }
}

