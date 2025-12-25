namespace Voya.Application.DTOs.Nexus;

public class NexusAssetListItemDto
{
	public Guid Id { get; set; }
	public string OriginalFileName { get; set; } = "";
	public string ContentType { get; set; } = "";
	public long SizeBytes { get; set; }
	public string Url { get; set; } = "";
	public string Folder { get; set; } = "";
	public DateTime CreatedAt { get; set; }
}

public class NexusAssetDetailDto
{
	public Guid Id { get; set; }
	public string OriginalFileName { get; set; } = "";
	public string StoredFileName { get; set; } = "";
	public string ContentType { get; set; } = "";
	public long SizeBytes { get; set; }
	public string Extension { get; set; } = "";
	public string HashSha256 { get; set; } = "";
	public string Url { get; set; } = "";
	public string Folder { get; set; } = "";
	public string TagsJson { get; set; } = "[]";
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }
}

public class NexusUploadAssetResponse
{
	public Guid Id { get; set; }
	public string Url { get; set; } = "";
}
