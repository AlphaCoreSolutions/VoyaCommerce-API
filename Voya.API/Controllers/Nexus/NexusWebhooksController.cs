using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/webhooks")]
public class NexusWebhooksController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public NexusWebhooksController(VoyaDbContext context) { _context = context; }

	// FEATURE 6: WEBHOOK MANAGER
	[HttpPost]
	public async Task<IActionResult> RegisterWebhook([FromBody] WebhookSubscription sub)
	{
		_context.WebhookSubscriptions.Add(sub);
		await _context.SaveChangesAsync();
		return Ok(sub);
	}
}