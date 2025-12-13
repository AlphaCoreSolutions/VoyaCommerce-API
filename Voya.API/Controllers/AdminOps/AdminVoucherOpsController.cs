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
[Route("api/v1/admin/vouchers")]
public class AdminVoucherOpsController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public AdminVoucherOpsController(VoyaDbContext context) { _context = context; }

	[HttpGet]
	[RequirePermission(Permissions.ContentManage)]
	public async Task<IActionResult> GetAllGlobalCoupons()
	{
		return Ok(await _context.GlobalCoupons.ToListAsync());
	}

	[HttpPost]
	[RequirePermission(Permissions.ContentManage)]
	public async Task<IActionResult> CreateGlobalCoupon([FromBody] GlobalCoupon coupon)
	{
		// Ensure code is uppercase
		coupon.Code = coupon.Code.ToUpper();

		// Ensure it's not a duplicate
		if (await _context.GlobalCoupons.AnyAsync(c => c.Code == coupon.Code))
			return BadRequest("Coupon code exists.");

		// Assign Creator (Admin)
		// coupon.CreatedByAdminId = GetUserId(); 

		_context.GlobalCoupons.Add(coupon);
		await _context.SaveChangesAsync();
		return Ok(coupon);
	}

	[HttpDelete("{id}")]
	[RequirePermission(Permissions.ContentManage)]
	public async Task<IActionResult> DeleteCoupon(Guid id)
	{
		var c = await _context.GlobalCoupons.FindAsync(id);
		if (c == null) return NotFound();
		_context.GlobalCoupons.Remove(c);
		await _context.SaveChangesAsync();
		return Ok("Deleted.");
	}
}