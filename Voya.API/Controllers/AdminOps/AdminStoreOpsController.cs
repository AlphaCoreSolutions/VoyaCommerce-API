using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.API.Attributes;
using Voya.Core.Constants;
using Voya.Core.Entities;
using Voya.Core.Enums;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.AdminOps;

[Authorize]
[ApiController]
[Route("api/v1/admin/stores")]
public class AdminStoreOpsController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public AdminStoreOpsController(VoyaDbContext context) { _context = context; }

	// 1. View Pending Applications
	[HttpGet("pending")]
	[RequirePermission(Permissions.StoresView)]
	public async Task<IActionResult> GetPendingStores()
	{
		var pending = await _context.Stores
			.Include(s => s.Owner)
			.Where(s => s.Status == StoreStatus.Pending)
			.OrderBy(s => s.CreatedAt)
			.ToListAsync();

		return Ok(pending);
	}

	// 2. Approve/Reject Logic
	[HttpPost("{storeId}/decide")]
	[RequirePermission(Permissions.StoresApprove)]
	public async Task<IActionResult> DecideStore(Guid storeId, [FromBody] DecisionDto request)
	{
		var store = await _context.Stores.FindAsync(storeId);
		if (store == null) return NotFound();

		if (request.Approved)
		{
			store.Status = StoreStatus.Active;
			// Notify Store Owner: "Welcome to Voya!"
		}
		else
		{
			store.Status = StoreStatus.Rejected;
			// Notify Store Owner: "Application Rejected: " + request.Reason
		}

		await _context.SaveChangesAsync();
		return Ok($"Store {store.Name} is now {store.Status}");
	}

	// FEATURE 5: STORE STRIKE SYSTEM
	[HttpPost("{storeId}/strike")]
	[RequirePermission(Permissions.StoresManage)]
	public async Task<IActionResult> AddStrike(Guid storeId, [FromBody] string reason)
	{
		var store = await _context.Stores.FindAsync(storeId);
		if (store == null) return NotFound();

		// 1. Add Strike
		var strike = new StoreStrike
		{
			StoreId = storeId,
			Reason = reason,
			IssuedByAdminId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value)
		};
		_context.StoreStrikes.Add(strike);

		// 2. Check Count
		var count = await _context.StoreStrikes.CountAsync(s => s.StoreId == storeId) + 1; // +1 includes this one

		string message = "Strike added.";

		if (count >= 3)
		{
			store.Status = Voya.Core.Enums.StoreStatus.Rejected; // Or Suspended
			message = "Strike added. Store has been AUTO-SUSPENDED due to 3 strikes.";
		}

		await _context.SaveChangesAsync();
		return Ok(message);
	}
}

public class DecisionDto { public bool Approved { get; set; } public string Reason { get; set; } = ""; }