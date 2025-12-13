namespace Voya.Core.Entities;

public class SystemReport
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Name { get; set; } = string.Empty; // e.g. "Financial_Q3_2023.pdf"
	public string Type { get; set; } = string.Empty; // "Financial P&L"
	public string GeneratedBy { get; set; } = "System"; // User Name
	public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
	public string DownloadUrl { get; set; } = string.Empty; // Mock URL
}