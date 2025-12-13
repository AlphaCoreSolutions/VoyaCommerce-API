using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

[Authorize]
[ApiController]
[Route("api/v1/nexus/logistics-config")]
public class NexusLogisticsConfigController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public NexusLogisticsConfigController(VoyaDbContext context) { _context = context; }

	[HttpGet]
	public async Task<IActionResult> GetProviders() => Ok(await _context.LogisticsProviders.ToListAsync());

	[HttpPost]
	public async Task<IActionResult> AddProvider([FromBody] LogisticsProvider provider)
	{
		// If setting this to active, deactivate others
		if (provider.IsActive)
		{
			var others = await _context.LogisticsProviders.ToListAsync();
			others.ForEach(p => p.IsActive = false);
		}

		_context.LogisticsProviders.Add(provider);
		await _context.SaveChangesAsync();
		return Ok("Provider added.");
	}

	[HttpPost("switch/{id}")]
	public async Task<IActionResult> SetActiveProvider(Guid id)
	{
		var providers = await _context.LogisticsProviders.ToListAsync();
		providers.ForEach(p => p.IsActive = (p.Id == id));
		await _context.SaveChangesAsync();
		return Ok("Logistics provider switched successfully.");
	}
}