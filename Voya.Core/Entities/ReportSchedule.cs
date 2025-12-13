namespace Voya.Core.Entities;

public enum ReportFrequency { Daily, Weekly, Monthly }

public class ReportSchedule
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string ReportName { get; set; } = string.Empty; // "Weekly Revenue"
	public ReportFrequency Frequency { get; set; }
	public string RecipientEmail { get; set; } = string.Empty;
	public DateTime LastSent { get; set; }
}