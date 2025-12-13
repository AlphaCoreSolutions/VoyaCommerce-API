namespace Voya.Core.Entities;

public enum RefundStatus { Pending, Scheduled, Completed, Cancelled }

public class CashRefundAppointment
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid UserId { get; set; } // The customer
	public Guid OrderId { get; set; }
	public decimal Amount { get; set; }

	// Scheduling Details
	public DateTime? ScheduledTime { get; set; }
	public string Location { get; set; } = "Voya HQ, Finance Dept, Floor 3";
	public string AdminNotes { get; set; } = string.Empty;

	public RefundStatus Status { get; set; } = RefundStatus.Pending;
	public Guid? AssignedAdminId { get; set; } // The specific role/employee handling this
}
