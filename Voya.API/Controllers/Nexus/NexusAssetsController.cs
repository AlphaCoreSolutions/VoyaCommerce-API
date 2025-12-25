using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Voya.API.Attributes;
using Voya.Core.Constants;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;
using Voya.Application.DTOs.Nexus;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/assets")]
public class NexusAssetsController : ControllerBase
{
	private readonly VoyaDbContext _context;
	private readonly IWebHostEnvironment _env;

	public NexusAssetsController(VoyaDbContext context, IWebHostEnvironment env)
	{
		_context = context;
		_env = env;
	}

	// GET /api/v1/nexus/assets?q=&type=&folder=&page=&pageSize=
	[HttpGet]
	[RequirePermission(Permissions.AssetsView)]
	public async Task<IActionResult> GetAssets(
		[FromQuery] string? q,
		[FromQuery] string? type,
		[FromQuery] string? folder,
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 24)
	{
		if (page < 1) page = 1;
		if (pageSize < 1) pageSize = 24;
		if (pageSize > 200) pageSize = 200;

		var query = _context.AssetFiles
			.AsNoTracking()
			.Where(a => !a.IsDeleted)
			.OrderByDescending(a => a.CreatedAt)
			.AsQueryable();

		if (!string.IsNullOrWhiteSpace(folder))
		{
			folder = folder.Trim();
			query = query.Where(a => a.Folder == folder);
		}

		if (!string.IsNullOrWhiteSpace(type))
		{
			type = type.Trim().ToLowerInvariant();
			// simple filter: "image", "pdf", "video", or full content-type like "image/png"
			if (type.Contains("/"))
				query = query.Where(a => a.ContentType == type);
			else if (type == "image")
				query = query.Where(a => a.ContentType.StartsWith("image/"));
			else if (type == "pdf")
				query = query.Where(a => a.ContentType == "application/pdf");
			else if (type == "video")
				query = query.Where(a => a.ContentType.StartsWith("video/"));
		}

		if (!string.IsNullOrWhiteSpace(q))
		{
			q = q.Trim();
			query = query.Where(a =>
				a.OriginalFileName.Contains(q) ||
				a.StoredFileName.Contains(q) ||
				a.ContentType.Contains(q) ||
				a.Folder.Contains(q)
			);
		}

		var total = await query.CountAsync();

		var items = await query
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.Select(a => new NexusAssetListItemDto
			{
				Id = a.Id,
				OriginalFileName = a.OriginalFileName,
				ContentType = a.ContentType,
				SizeBytes = a.SizeBytes,
				Url = a.Url,
				Folder = a.Folder,
				CreatedAt = a.CreatedAt
			})
			.ToListAsync();

		return Ok(new NexusPagedResult<NexusAssetListItemDto>
		{
			Items = items,
			Page = page,
			PageSize = pageSize,
			TotalCount = total
		});
	}

	// GET /api/v1/nexus/assets/{id}
	[HttpGet("{id:guid}")]
	[RequirePermission(Permissions.AssetsView)]
	public async Task<IActionResult> GetAsset(Guid id)
	{
		var a = await _context.AssetFiles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
		if (a == null) return NotFound();

		var dto = new NexusAssetDetailDto
		{
			Id = a.Id,
			OriginalFileName = a.OriginalFileName,
			StoredFileName = a.StoredFileName,
			ContentType = a.ContentType,
			SizeBytes = a.SizeBytes,
			Extension = a.Extension,
			HashSha256 = a.HashSha256,
			Url = a.Url,
			Folder = a.Folder,
			TagsJson = a.TagsJson,
			CreatedAt = a.CreatedAt,
			UpdatedAt = a.UpdatedAt
		};

		return Ok(dto);
	}

	// POST /api/v1/nexus/assets/upload
	// multipart/form-data:
	// file: IFormFile (required)
	// folder: string (optional) e.g. "cms"
	// tagsJson: string (optional) e.g. ["hero","banner"]
	[HttpPost("upload")]
	[RequestSizeLimit(50_000_000)] // 50MB
	[RequirePermission(Permissions.AssetsManage)]
	public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] string? folder, [FromForm] string? tagsJson)
	{
		if (file == null || file.Length == 0) return BadRequest("File is required.");

		folder = string.IsNullOrWhiteSpace(folder) ? "" : folder.Trim();

		var ext = Path.GetExtension(file.FileName) ?? "";
		var storedName = $"{Guid.NewGuid():N}{ext}";
		var uploadsRoot = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads", folder);

		Directory.CreateDirectory(uploadsRoot);

		var fullPath = Path.Combine(uploadsRoot, storedName);

		// Save file
		await using (var fs = System.IO.File.Create(fullPath))
		{
			await file.CopyToAsync(fs);
		}

		// Hash SHA256
		var sha256 = await ComputeSha256Async(fullPath);

		// Build public URL (served from wwwroot)
		var baseUrl = $"{Request.Scheme}://{Request.Host}";
		var urlPath = string.IsNullOrEmpty(folder)
			? $"/uploads/{storedName}"
			: $"/uploads/{folder}/{storedName}";

		var asset = new AssetFile
		{
			OriginalFileName = file.FileName,
			StoredFileName = storedName,
			ContentType = file.ContentType ?? "application/octet-stream",
			SizeBytes = file.Length,
			Extension = ext,
			HashSha256 = sha256,
			Url = $"{baseUrl}{urlPath}",
			Folder = folder,
			TagsJson = string.IsNullOrWhiteSpace(tagsJson) ? "[]" : tagsJson,
			IsDeleted = false,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow,
			UploadedByUserId = TryGetUserId()
		};

		_context.AssetFiles.Add(asset);
		await _context.SaveChangesAsync();

		return Ok(new NexusUploadAssetResponse { Id = asset.Id, Url = asset.Url });
	}

	// DELETE /api/v1/nexus/assets/{id} (soft delete)
	[HttpDelete("{id:guid}")]
	[RequirePermission(Permissions.AssetsManage)]
	public async Task<IActionResult> Delete(Guid id)
	{
		var a = await _context.AssetFiles.FirstOrDefaultAsync(x => x.Id == id);
		if (a == null) return NotFound();

		a.IsDeleted = true;
		a.UpdatedAt = DateTime.UtcNow;
		await _context.SaveChangesAsync();

		return Ok(new { a.Id, a.IsDeleted });
	}

	// POST /api/v1/nexus/assets/{id}/restore
	[HttpPost("{id:guid}/restore")]
	[RequirePermission(Permissions.AssetsManage)]
	public async Task<IActionResult> Restore(Guid id)
	{
		var a = await _context.AssetFiles.FirstOrDefaultAsync(x => x.Id == id);
		if (a == null) return NotFound();

		a.IsDeleted = false;
		a.UpdatedAt = DateTime.UtcNow;
		await _context.SaveChangesAsync();

		return Ok(new { a.Id, a.IsDeleted });
	}

	private async Task<string> ComputeSha256Async(string filePath)
	{
		await using var stream = System.IO.File.OpenRead(filePath);
		using var sha = SHA256.Create();
		var hash = await sha.ComputeHashAsync(stream);
		return Convert.ToHexString(hash).ToLowerInvariant();
	}

	private Guid? TryGetUserId()
	{
		var raw = User.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == "id")?.Value;
		return Guid.TryParse(raw, out var id) ? id : null;
	}
}
