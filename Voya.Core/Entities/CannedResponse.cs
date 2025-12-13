namespace Voya.Core.Entities;

public class CannedResponse
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Shortcut { get; set; } = string.Empty; // e.g., "refund-approved"
	public string Content { get; set; } = string.Empty; // The full message
	public string Category { get; set; } = "General";
}