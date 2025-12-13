namespace Voya.Core.Entities;

public enum AppType { UserApp, StoreApp, DriverApp, All }

public class Announcement
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Title { get; set; } = string.Empty;
	public string Message { get; set; } = string.Empty;
	public AppType TargetApp { get; set; }
	public string ColorHex { get; set; } = "#FF0000"; // Alert color
	public DateTime StartDate { get; set; }
	public DateTime EndDate { get; set; }
	public bool IsActive { get; set; } = true;
}