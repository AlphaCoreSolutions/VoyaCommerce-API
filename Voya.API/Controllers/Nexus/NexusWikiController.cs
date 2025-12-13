using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.API.Attributes;
using Voya.Core.Constants;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/wiki")]
public class NexusWikiController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public NexusWikiController(VoyaDbContext context) { _context = context; }

	// GET: api/v1/nexus/wiki
	[HttpGet]
	public async Task<IActionResult> GetArticles()
	{
		var articles = await _context.WikiArticles
			.OrderBy(a => a.Category)
			.ThenBy(a => a.Title)
			.ToListAsync();
		return Ok(articles);
	}

	// POST: Create Article
	[HttpPost]
	[RequirePermission(Permissions.ContentManage)]
	public async Task<IActionResult> CreateArticle([FromBody] WikiArticle article)
	{
		article.Id = Guid.NewGuid();
		article.LastUpdated = DateTime.UtcNow;
		_context.WikiArticles.Add(article);
		await _context.SaveChangesAsync();
		return Ok(article);
	}

	// PUT: Update Article
	[HttpPut("{id}")]
	[RequirePermission(Permissions.ContentManage)]
	public async Task<IActionResult> UpdateArticle(Guid id, [FromBody] WikiArticle article)
	{
		var existing = await _context.WikiArticles.FindAsync(id);
		if (existing == null) return NotFound();

		existing.Title = article.Title;
		existing.Category = article.Category;
		existing.Content = article.Content;
		existing.LastUpdated = DateTime.UtcNow;

		await _context.SaveChangesAsync();
		return Ok(existing);
	}
}