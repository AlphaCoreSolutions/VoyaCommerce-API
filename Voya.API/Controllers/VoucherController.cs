using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.Application.DTOs;
using Voya.Core.Entities;
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

	private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")!.Value);

	[HttpGet]
	public async Task<ActionResult<List<VoucherDto>>> GetMyVouchers()
	{
		var userId = GetUserId();
		var now = DateTime.UtcNow;

		// Get vouchers claimed by user that are still valid
		var vouchers = await _context.UserVouchers
			.Include(uv => uv.Voucher)
			.Where(uv => uv.UserId == userId &&
						 uv.Voucher.IsActive &&
						 uv.Voucher.EndDate > now &&
						 uv.UsageCount < uv.Voucher.MaxUsesPerUser) // Only show if usable
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

	[HttpPost("claim")]
	public async Task<IActionResult> ClaimVoucher(ClaimVoucherRequest request)
	{
		var userId = GetUserId();
		var now = DateTime.UtcNow;

		// 1. Find Voucher
		var voucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.Code == request.Code);

		if (voucher == null) return NotFound("Invalid voucher code.");
		if (!voucher.IsActive || voucher.EndDate < now) return BadRequest("This voucher has expired.");

		// 2. Check if already claimed
		var existingClaim = await _context.UserVouchers
			.FirstOrDefaultAsync(uv => uv.UserId == userId && uv.VoucherId == voucher.Id);

		if (existingClaim != null)
		{
			return BadRequest("You have already claimed this voucher.");
		}

		// 3. Add to Wallet
		var userVoucher = new UserVoucher
		{
			UserId = userId,
			VoucherId = voucher.Id,
			UsageCount = 0 // Not used yet
		};

		_context.UserVouchers.Add(userVoucher);
		await _context.SaveChangesAsync();

		return Ok(new { Message = "Voucher claimed successfully!" });
	}
}