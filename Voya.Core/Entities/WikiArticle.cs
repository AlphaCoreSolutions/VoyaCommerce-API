namespace Voya.Core.Entities;

public class WikiArticle
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Title { get; set; } = string.Empty;
	public string Category { get; set; } = "General"; // e.g. "SOP", "Refunds"
	public string Content { get; set; } = string.Empty; // Markdown or Text
	public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
	public string Author { get; set; } = "Admin";
}