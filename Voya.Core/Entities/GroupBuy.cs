namespace Voya.Core.Entities;

public class GroupBuy
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid ProductId { get; set; }
	public Guid InitiatorUserId { get; set; }

	public bool IsCompleted { get; set; } = false;
	public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);

	// Who joined?
	public List<string> ParticipantOrderIds { get; set; } = new();
}