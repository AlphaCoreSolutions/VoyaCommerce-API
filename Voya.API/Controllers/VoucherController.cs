using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.Application.DTOs;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/vouchers")]
public class VoucherController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public VoucherController(VoyaDbContext context)
	{
		_context = context;
	}

	private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

	[HttpGet]
	public async Task<ActionResult<List<VoucherDto>>> GetMyVouchers()
	{
		var userId = GetUserId();
		var now = DateTime.UtcNow;

		var vouchers = await _context.UserVouchers
			.Include(uv => uv.Voucher)
			.Where(uv => uv.UserId == userId &&
						 uv.Voucher.IsActive &&
						 uv.Voucher.EndDate > now &&
						 uv.UsageCount < uv.Voucher.MaxUsesPerUser)
			.OrderByDescending(uv => uv.DateClaimed) // Show newest first
			.Select(uv => new VoucherDto(
				uv.Voucher.Id,
				uv.Voucher.Code,
				uv.Voucher.Description,
				uv.Voucher.Value,
				uv.Voucher.Type.ToString(),
				uv.Voucher.EndDate
			))
			.ToListAsync();

		return Ok(vouchers);
	}
}