namespace Voya.Core.Entities;

public class PayrollRecord
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid StaffUserId { get; set; }
	public int Month { get; set; }
	public int Year { get; set; }
	public decimal TotalHoursWorked { get; set; }
	public decimal HourlyRate { get; set; }
	public decimal TotalPayout { get; set; }
	public bool IsPaid { get; set; } = false;
}