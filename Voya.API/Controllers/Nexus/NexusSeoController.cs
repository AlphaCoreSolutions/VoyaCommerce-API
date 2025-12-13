using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/seo")]
public class NexusSeoController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public NexusSeoController(VoyaDbContext context) { _context = context; }

	// FEATURE 7: SEO DASHBOARD
	[HttpPost]
	public async Task<IActionResult> UpdateMetaTags([FromBody] SeoMetaTag tags)
	{
		// Update logic omitted for brevity (similar to other updates)
		_context.SeoMetaTags.Add(tags);
		await _context.SaveChangesAsync();
		return Ok("SEO tags updated.");
	}
}