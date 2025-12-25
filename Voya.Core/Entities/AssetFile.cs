namespace Voya.Core.Entities;

public class AssetFile
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public string OriginalFileName { get; set; } = string.Empty;
	public string StoredFileName { get; set; } = string.Empty;    // e.g. GUID.ext
	public string ContentType { get; set; } = string.Empty;       // image/png, application/pdf
	public long SizeBytes { get; set; }

	public string Extension { get; set; } = string.Empty;         // .png
	public string HashSha256 { get; set; } = string.Empty;         // optional but useful
	public string Url { get; set; } = string.Empty;               // public URL

	// Optional organization
	public string Folder { get; set; } = "";                      // e.g. "cms", "products"
	public string TagsJson { get; set; } = "[]";                  // ["banner","hero"]

	public bool IsDeleted { get; set; }
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

	public Guid? UploadedByUserId { get; set; }
}
