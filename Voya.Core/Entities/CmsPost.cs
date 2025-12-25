namespace Voya.Core.Entities;

public class CmsPost
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public string Title { get; set; } = string.Empty;
	public string Slug { get; set; } = string.Empty;

	// Main content
	public string ContentHtml { get; set; } = string.Empty;

	// Optional summary for cards/listing
	public string Excerpt { get; set; } = string.Empty;

	// Cover image
	public string CoverImageUrl { get; set; } = string.Empty;

	// SEO
	public string SeoTitle { get; set; } = string.Empty;
	public string SeoDescription { get; set; } = string.Empty;

	// Tags stored as JSON array string ["tag1","tag2"]
	public string TagsJson { get; set; } = "[]";

	public bool IsPublished { get; set; }
	public DateTime? PublishedAt { get; set; }

	public bool IsDeleted { get; set; }
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

	// Who created it (Nexus admin user id, optional)
	public Guid? CreatedByUserId { get; set; }
}
