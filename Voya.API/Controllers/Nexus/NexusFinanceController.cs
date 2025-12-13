using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.API.Attributes;
using Voya.Core.Constants;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/finance")]
public class NexusFinanceController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public NexusFinanceController(VoyaDbContext context) { _context = context; }

	[HttpGet("payouts/pending")]
	public async Task<IActionResult> GetPendingPayouts()
	{
		var requests = await _context.PayoutRequests
			.Where(p => !p.IsProcessed)
			.OrderBy(p => p.RequestedAt)
			.ToListAsync(); // In real app, include Store name

		return Ok(requests);
	}

	[HttpPost("payouts/{id}/approve")]
	[RequirePermission(Permissions.FinancePayout)]
	public async Task<IActionResult> ApprovePayout(Guid id)
	{
		var request = await _context.PayoutRequests.FindAsync(id);
		if (request == null) return NotFound();

		request.IsProcessed = true;
		await _context.SaveChangesAsync();

		return Ok($"Payout of {request.Amount} approved.");
	}
}