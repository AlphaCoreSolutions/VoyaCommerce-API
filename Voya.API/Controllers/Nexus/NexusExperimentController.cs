using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/experiments")]
public class NexusExperimentsController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public NexusExperimentsController(VoyaDbContext context) { _context = context; }

	// FEATURE 4: A/B TESTING FRAMEWORK
	[HttpPost]
	public async Task<IActionResult> CreateExperiment([FromBody] AbTestExperiment exp)
	{
		_context.AbTestExperiments.Add(exp);
		await _context.SaveChangesAsync();
		return Ok(exp);
	}

	[HttpGet("config")]
	[AllowAnonymous] // App calls this on startup
	public async Task<IActionResult> GetActiveExperiments()
	{
		// Returns list of active tests so app can randomize based on User ID
		return Ok(await _context.AbTestExperiments.Where(e => e.IsActive).ToListAsync());
	}
}