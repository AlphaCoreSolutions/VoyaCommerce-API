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
[Route("api/v1/admin/verification")]
public class AdminVerificationController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public AdminVerificationController(VoyaDbContext context) { _context = context; }

	[HttpGet("pending")]
	[RequirePermission(Permissions.StoresView)]
	public async Task<IActionResult> GetPending()
	{
		return Ok(await _context.SellerVerifications
			.Where(v => v.Status == VerificationStatus.Pending)
			.ToListAsync());
	}

	[HttpPost("{id}/decide")]
	[RequirePermission(Permissions.StoresManage)]
	public async Task<IActionResult> Decide(Guid id, [FromBody] bool approved)
	{
		var req = await _context.SellerVerifications.FindAsync(id);
		if (req == null) return NotFound();

		req.Status = approved ? VerificationStatus.Approved : VerificationStatus.Rejected;

		// If approved, maybe set a flag on the Store entity too
		// var store = await _context.Stores.FindAsync(req.StoreId);
		// store.IsVerified = true;

		await _context.SaveChangesAsync();
		return Ok($"Request {(approved ? "Approved" : "Rejected")}");
	}
}