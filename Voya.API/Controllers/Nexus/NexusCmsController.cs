using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Voya.API.Attributes;
using Voya.Core.Constants;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;
using Voya.Application.DTOs.Nexus;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/cms")]
public class NexusCmsController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public NexusCmsController(VoyaDbContext context)
	{
		_context = context;
	}

	// GET /api/v1/nexus/cms/posts?published=true/false&q=...&page=1&pageSize=20
	[HttpGet("posts")]
	[RequirePermission(Permissions.CmsView)]
	public async Task<IActionResult> GetPosts(
		[FromQuery] bool? published,
		[FromQuery] string? q,
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 20)
	{
		if (page < 1) page = 1;
		if (pageSize < 1) pageSize = 20;
		if (pageSize > 200) pageSize = 200;

		var query = _context.CmsPosts
			.AsNoTracking()
			.Where(p => !p.IsDeleted)
			.OrderByDescending(p => p.UpdatedAt)
			.AsQueryable();

		if (published.HasValue)
			query = query.Where(p => p.IsPublished == published.Value);

		if (!string.IsNullOrWhiteSpace(q))
		{
			q = q.Trim();
			query = query.Where(p =>
				p.Title.Contains(q) ||
				p.Slug.Contains(q) ||
				p.Excerpt.Contains(q)
			);
		}

		var total = await query.CountAsync();

		var items = await query
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.Select(p => new NexusCmsPostListItemDto
			{
				Id = p.Id,
				Title = p.Title,
				Slug = p.Slug,
				Excerpt = p.Excerpt,
				CoverImageUrl = p.CoverImageUrl,
				IsPublished = p.IsPublished,
				PublishedAt = p.PublishedAt,
				UpdatedAt = p.UpdatedAt
			})
			.ToListAsync();

		return Ok(new NexusPagedResult<NexusCmsPostListItemDto>
		{
			Items = items,
			Page = page,
			PageSize = pageSize,
			TotalCount = total
		});
	}

	// GET /api/v1/nexus/cms/posts/{id}
	[HttpGet("posts/{id:guid}")]
	[RequirePermission(Permissions.CmsView)]
	public async Task<IActionResult> GetPost(Guid id)
	{
		var p = await _context.CmsPosts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
		if (p == null) return NotFound();

		return Ok(new NexusCmsPostDetailDto
		{
			Id = p.Id,
			Title = p.Title,
			Slug = p.Slug,
			ContentHtml = p.ContentHtml,
			Excerpt = p.Excerpt,
			CoverImageUrl = p.CoverImageUrl,
			SeoTitle = p.SeoTitle,
			SeoDescription = p.SeoDescription,
			TagsJson = p.TagsJson,
			IsPublished = p.IsPublished,
			PublishedAt = p.PublishedAt,
			CreatedAt = p.CreatedAt,
			UpdatedAt = p.UpdatedAt
		});
	}

	// POST /api/v1/nexus/cms/posts
	[HttpPost("posts")]
	[RequirePermission(Permissions.CmsManage)]
	public async Task<IActionResult> Create([FromBody] NexusUpsertCmsPostRequest req)
	{
		var slug = string.IsNullOrWhiteSpace(req.Slug)
			? GenerateSlug(req.Title)
			: GenerateSlug(req.Slug);

		// ensure slug unique
		slug = await MakeUniqueSlug(slug);

		var post = new CmsPost
		{
			Title = req.Title.Trim(),
			Slug = slug,
			ContentHtml = req.ContentHtml ?? "",
			Excerpt = req.Excerpt ?? "",
			CoverImageUrl = req.CoverImageUrl ?? "",
			SeoTitle = req.SeoTitle ?? "",
			SeoDescription = req.SeoDescription ?? "",
			TagsJson = req.TagsJson ?? "[]",
			IsPublished = false,
			PublishedAt = null,
			IsDeleted = false,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow,
			CreatedByUserId = TryGetUserId()
		};

		_context.CmsPosts.Add(post);
		await _context.SaveChangesAsync();
		return Ok(new { post.Id });
	}

	// PUT /api/v1/nexus/cms/posts/{id}
	[HttpPut("posts/{id:guid}")]
	[RequirePermission(Permissions.CmsManage)]
	public async Task<IActionResult> Update(Guid id, [FromBody] NexusUpsertCmsPostRequest req)
	{
		var post = await _context.CmsPosts.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
		if (post == null) return NotFound();

		post.Title = req.Title.Trim();
		post.ContentHtml = req.ContentHtml ?? "";
		post.Excerpt = req.Excerpt ?? "";
		post.CoverImageUrl = req.CoverImageUrl ?? "";
		post.SeoTitle = req.SeoTitle ?? "";
		post.SeoDescription = req.SeoDescription ?? "";
		post.TagsJson = req.TagsJson ?? "[]";

		if (!string.IsNullOrWhiteSpace(req.Slug))
		{
			var desiredSlug = GenerateSlug(req.Slug);
			if (!string.Equals(desiredSlug, post.Slug, StringComparison.OrdinalIgnoreCase))
			{
				post.Slug = await MakeUniqueSlug(desiredSlug, post.Id);
			}
		}

		post.UpdatedAt = DateTime.UtcNow;
		await _context.SaveChangesAsync();

		return Ok(new { post.Id });
	}

	// POST /api/v1/nexus/cms/posts/{id}/publish
	[HttpPost("posts/{id:guid}/publish")]
	[RequirePermission(Permissions.CmsManage)]
	public async Task<IActionResult> Publish(Guid id)
	{
		var post = await _context.CmsPosts.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
		if (post == null) return NotFound();

		post.IsPublished = true;
		post.PublishedAt ??= DateTime.UtcNow;
		post.UpdatedAt = DateTime.UtcNow;

		await _context.SaveChangesAsync();
		return Ok(new { post.Id, post.IsPublished, post.PublishedAt });
	}

	// POST /api/v1/nexus/cms/posts/{id}/unpublish
	[HttpPost("posts/{id:guid}/unpublish")]
	[RequirePermission(Permissions.CmsManage)]
	public async Task<IActionResult> Unpublish(Guid id)
	{
		var post = await _context.CmsPosts.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
		if (post == null) return NotFound();

		post.IsPublished = false;
		post.UpdatedAt = DateTime.UtcNow;

		await _context.SaveChangesAsync();
		return Ok(new { post.Id, post.IsPublished });
	}

	// DELETE /api/v1/nexus/cms/posts/{id}
	[HttpDelete("posts/{id:guid}")]
	[RequirePermission(Permissions.CmsManage)]
	public async Task<IActionResult> Delete(Guid id)
	{
		var post = await _context.CmsPosts.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
		if (post == null) return NotFound();

		post.IsDeleted = true;
		post.UpdatedAt = DateTime.UtcNow;

		await _context.SaveChangesAsync();
		return Ok(new { post.Id, post.IsDeleted });
	}

	// -----------------------------
	// Helpers
	// -----------------------------
	private static string GenerateSlug(string input)
	{
		input = input.ToLowerInvariant().Trim();
		input = Regex.Replace(input, @"[^a-z0-9\s-]", "");
		input = Regex.Replace(input, @"\s+", "-").Trim('-');
		input = Regex.Replace(input, @"-+", "-");
		return string.IsNullOrWhiteSpace(input) ? "post" : input;
	}

	private async Task<string> MakeUniqueSlug(string slug, Guid? ignoreId = null)
	{
		var baseSlug = slug;
		var i = 1;

		while (await _context.CmsPosts.AnyAsync(p =>
				   !p.IsDeleted &&
				   p.Slug == slug &&
				   (!ignoreId.HasValue || p.Id != ignoreId.Value)))
		{
			slug = $"{baseSlug}-{i}";
			i++;
		}

		return slug;
	}

	private Guid? TryGetUserId()
	{
		// If you store user id in claim "sub" or "id", adjust this accordingly.
		var raw = User.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == "id")?.Value;
		return Guid.TryParse(raw, out var id) ? id : null;
	}
}
