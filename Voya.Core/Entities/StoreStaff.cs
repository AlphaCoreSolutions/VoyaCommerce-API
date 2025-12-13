namespace Voya.Core.Entities;

public enum StoreRole { Manager, Editor, Viewer }

public class StoreStaff
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid StoreId { get; set; }

	// Link to an existing Voya User (they must register first)
	public Guid UserId { get; set; }
	public User User { get; set; } = null!;

	public StoreRole Role { get; set; } = StoreRole.Viewer;
	public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}