namespace Voya.Core.Entities;

public class NexusRole
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Name { get; set; } = string.Empty; // e.g., "Support Team", "Logistics Manager"
	public string Description { get; set; } = string.Empty;
	public bool IsSuperAdmin { get; set; } = false; // "God Mode" - bypasses checks

	// JSON List of permission strings: ["users.view", "stores.manage", "orders.refund"]
	public List<string> Permissions { get; set; } = new();

	public ICollection<User> Users { get; set; } = new List<User>();
}