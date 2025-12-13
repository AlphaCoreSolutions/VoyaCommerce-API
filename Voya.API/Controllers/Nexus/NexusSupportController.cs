using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/support")]
public class NexusSupportController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public NexusSupportController(VoyaDbContext context) { _context = context; }

	// FEATURE 8: TICKET ASSIGNMENT RULES
	[HttpPost("rules")]
	public async Task<IActionResult> CreateRule([FromBody] SupportAssignmentRule rule)
	{
		_context.SupportAssignmentRules.Add(rule);
		await _context.SaveChangesAsync();
		return Ok("Rule created.");
	}
}