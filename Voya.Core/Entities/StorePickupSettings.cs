namespace Voya.Core.Entities;

public class StorePickupSettings
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid StoreId { get; set; }

	// Config: "Ready between 2 PM - 4 PM"
	// We store windows as a list of strings or structured times. 
	// Simple approach: List of available hourly slots.
	public bool IsPickupEnabled { get; set; } = true;
	public string Instructions { get; set; } = "Call upon arrival.";
}