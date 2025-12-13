namespace Voya.Core.Entities;

public class StaffShift
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid StoreId { get; set; }
	public Guid StaffUserId { get; set; }

	public DateTime ClockInTime { get; set; } = DateTime.UtcNow;
	public DateTime? ClockOutTime { get; set; }

	public decimal TotalHours => ClockOutTime.HasValue
		? (decimal)(ClockOutTime.Value - ClockInTime).TotalHours
		: 0;
}