using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voya.API.Attributes;
using Voya.Core.Constants;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/legal")]
public class NexusLegalController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public NexusLegalController(VoyaDbContext context) { _context = context; }

	// FEATURE 3: LEGAL DOCS
	[HttpPost("documents")]
	[RequirePermission(Permissions.ContentManage)]
	public async Task<IActionResult> PublishDocument([FromBody] LegalDocument doc)
	{
		doc.EffectiveDate = DateTime.UtcNow;
		_context.LegalDocuments.Add(doc);
		await _context.SaveChangesAsync();

		if (doc.ForceReacceptance)
		{
			// Logic: Could update a 'TermsAcceptedVersion' field on all Users to null
			// For now, we just return the flag
		}
		return Ok($"New {doc.Type} version {doc.Version} published.");
	}
}