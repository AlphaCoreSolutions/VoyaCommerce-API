namespace Voya.Core.Entities;

public class AppVersionConfig
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Platform { get; set; } = "Android"; // "iOS"
	public string MinVersion { get; set; } = "1.0.0"; // Users below this MUST update
	public string CurrentVersion { get; set; } = "1.2.0"; // Latest available
	public string UpdateMessage { get; set; } = "Please update to get new features.";
	public bool ForceUpdate { get; set; } = false;
}