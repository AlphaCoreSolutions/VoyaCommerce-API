using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.API.Attributes;
using Voya.Core.Constants;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.AdminOps;

[Authorize]
[ApiController]
[Route("api/v1/admin/content")]
public class AdminContentController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public AdminContentController(VoyaDbContext context) { _context = context; }

	// FEATURE 7: DYNAMIC FAQ MANAGER
	[HttpPost("faq")]
	[RequirePermission(Permissions.ContentManage)]
	public async Task<IActionResult> AddFaq([FromBody] FaqItem faq)
	{
		_context.FaqItems.Add(faq);
		await _context.SaveChangesAsync();
		return Ok("FAQ Added");
	}

	// FEATURE 2: PRODUCT SEO ANALYZER
	[HttpGet("seo-check/{productId}")]
	public async Task<IActionResult> AnalyzeProductSeo(Guid productId)
	{
		var p = await _context.Products.FindAsync(productId);
		if (p == null) return NotFound();

		int score = 0;
		var tips = new List<string>();

		if (p.Name.Length > 20) score += 20; else tips.Add("Name too short");
		if (p.Description.Length > 100) score += 30; else tips.Add("Description too short");
		if (p.Tags.Count >= 3) score += 20; else tips.Add("Add more tags");
		if (!string.IsNullOrEmpty(p.MainImageUrl)) score += 30;

		return Ok(new { Product = p.Name, Score = score, Tips = tips });
	}
}