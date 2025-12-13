namespace Voya.Core.Entities;

public class AdminInternalNote
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string TargetEntityId { get; set; } = string.Empty; // OrderId, UserId, TicketId
	public string TargetType { get; set; } = "User"; // Enum or String
	public string Note { get; set; } = string.Empty;
	public Guid WrittenByAdminId { get; set; }
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}