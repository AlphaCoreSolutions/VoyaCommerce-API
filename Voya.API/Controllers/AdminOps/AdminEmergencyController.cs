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
[Route("api/v1/admin/emergency")]
public class AdminEmergencyController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public AdminEmergencyController(VoyaDbContext context) { _context = context; }

	// FEATURE 7: ORDER FORCE ACTIONS
	[HttpPost("orders/{id}/force-status")]
	[RequirePermission(Permissions.SystemConfig)] // Restricted permission
	public async Task<IActionResult> ForceOrderStatus(Guid id, [FromBody] OrderStatus newStatus)
	{
		var order = await _context.Orders.FindAsync(id);
		if (order == null) return NotFound();

		// Audit this dangerous action!
		// _auditLog.Log("Force Status Change", order.Id, newStatus);

		order.Status = newStatus;
		await _context.SaveChangesAsync();
		return Ok($"Order status forced to {newStatus}");
	}

	// FEATURE 9: STORE IMPERSONATION
	[HttpPost("impersonate/store/{storeId}")]
	[RequirePermission(Permissions.StoresManage)]
	public async Task<IActionResult> ImpersonateStore(Guid storeId)
	{
		var store = await _context.Stores.Include(s => s.Owner).FirstOrDefaultAsync(s => s.Id == storeId);
		if (store == null) return NotFound();

		// In production, return a JWT for the Store Owner
		return Ok(new
		{
			Message = $"Impersonating Store: {store.Name}",
			OwnerEmail = store.Owner.Email,
			RedirectUrl = $"/seller/dashboard?mock_token=xyz"
		});
	}
}