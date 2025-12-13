namespace Voya.Core.Entities;

public class SeoMetaTag
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string PageRoute { get; set; } = "/home"; // The URL path
	public string Title { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public string Keywords { get; set; } = string.Empty;
}