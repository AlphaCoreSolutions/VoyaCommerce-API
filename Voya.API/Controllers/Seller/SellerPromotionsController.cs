using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Seller;

[Authorize]
[ApiController]
[Route("api/v1/seller/promotions")]
public class SellerPromotionsController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public SellerPromotionsController(VoyaDbContext context)
	{
		_context = context;
	}

	private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

	// --- FLASH SALES ---

	[HttpPost("flash-sale")]
	public async Task<IActionResult> ProposeFlashSale([FromBody] FlashSale request)
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);
		if (store == null) return Unauthorized();

		request.StoreId = store.Id;
		request.Status = FlashSaleStatus.PendingApproval; // Must be approved by Admin

		_context.FlashSales.Add(request);
		await _context.SaveChangesAsync();
		return Ok(new { Message = "Flash sale submitted for approval", Id = request.Id });
	}

	[HttpGet("flash-sales")]
	public async Task<IActionResult> GetMyFlashSales()
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);

		var sales = await _context.FlashSales
			.Where(f => f.StoreId == store!.Id)
			.OrderByDescending(f => f.StartTime)
			.ToListAsync();

		return Ok(sales);
	}

	// --- BUNDLES ---

	[HttpPost("bundle")]
	public async Task<IActionResult> CreateBundle([FromBody] ProductBundle request)
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);
		if (store == null) return Unauthorized();

		// Verify products belong to store
		var validProducts = await _context.Products
			.CountAsync(p => request.ProductIds.Contains(p.Id) && p.StoreId == store.Id);

		if (validProducts != request.ProductIds.Count)
			return BadRequest("One or more products do not belong to your store.");

		request.StoreId = store.Id;
		_context.ProductBundles.Add(request);
		await _context.SaveChangesAsync();

		return Ok(new { Message = "Bundle created", Id = request.Id });
	}
}