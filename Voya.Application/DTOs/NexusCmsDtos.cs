namespace Voya.Application.DTOs.Nexus;

public class NexusCmsPostListItemDto
{
	public Guid Id { get; set; }
	public string Title { get; set; } = "";
	public string Slug { get; set; } = "";
	public string Excerpt { get; set; } = "";
	public string CoverImageUrl { get; set; } = "";
	public bool IsPublished { get; set; }
	public DateTime? PublishedAt { get; set; }
	public DateTime UpdatedAt { get; set; }
}

public class NexusCmsPostDetailDto
{
	public Guid Id { get; set; }
	public string Title { get; set; } = "";
	public string Slug { get; set; } = "";
	public string ContentHtml { get; set; } = "";
	public string Excerpt { get; set; } = "";
	public string CoverImageUrl { get; set; } = "";
	public string SeoTitle { get; set; } = "";
	public string SeoDescription { get; set; } = "";
	public string TagsJson { get; set; } = "[]";
	public bool IsPublished { get; set; }
	public DateTime? PublishedAt { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }
}

public class NexusUpsertCmsPostRequest
{
	public string Title { get; set; } = "";
	public string? Slug { get; set; }          // optional; auto-generated if null
	public string ContentHtml { get; set; } = "";
	public string Excerpt { get; set; } = "";
	public string CoverImageUrl { get; set; } = "";
	public string SeoTitle { get; set; } = "";
	public string SeoDescription { get; set; } = "";
	public string TagsJson { get; set; } = "[]";
}
