namespace Voya.Core.Entities;

public class GeneratedReport
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string ReportName { get; set; } = string.Empty;
	public string FileType { get; set; } = "CSV";
	public string Content { get; set; } = string.Empty; // Store CSV text here
	public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}