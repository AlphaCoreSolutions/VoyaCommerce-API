namespace Voya.Core.Entities;

public enum TicketType { GeneralQuestion, RefundRequest, BugReport, AccountIssue, OrderProblem }
public enum TicketStatus { Open, InProgress, Resolved, Closed }
public enum TicketPriority { Low, Medium, High, Urgent }

public class Ticket
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid UserId { get; set; }

	// If related to a specific order (optional)
	public string? RelatedOrderId { get; set; }

	public TicketType Type { get; set; } = TicketType.GeneralQuestion;
	public TicketStatus Status { get; set; } = TicketStatus.Open;
	public TicketPriority Priority { get; set; } = TicketPriority.Medium;

	public string Subject { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;

	// Admin Response
	public string? AdminResponse { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public DateTime? ResolvedAt { get; set; }
}


public class TicketMessage
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid TicketId { get; set; }
	public string Message { get; set; } = string.Empty;
	public bool IsAdminReply { get; set; } // True = Admin, False = User
	public string SenderName { get; set; } = string.Empty;
	public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}