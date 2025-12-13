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
[Route("api/v1/wishlist")]
public class WishlistController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public WishlistController(VoyaDbContext context)
	{
		_context = context;
	}

	private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")!.Value);

	[HttpGet]
	public async Task<ActionResult<List<WishlistDto>>> GetWishlist()
	{
		var userId = GetUserId();
		var items = await _context.WishlistItems
			.Include(w => w.Product)
			.Where(w => w.UserId == userId)
			.Select(w => new WishlistDto(w.ProductId, w.Product.Name, w.Product.BasePrice, w.Product.MainImageUrl))
			.ToListAsync();
		return Ok(items);
	}

	[HttpPost("toggle/{productId}")]
	public async Task<IActionResult> ToggleWishlist(Guid productId)
	{
		var userId = GetUserId();
		var existing = await _context.WishlistItems.FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

		if (existing != null)
		{
			_context.WishlistItems.Remove(existing);
			await _context.SaveChangesAsync();
			return Ok(new { IsWishlisted = false, Message = "Removed from wishlist" });
		}
		else
		{
			_context.WishlistItems.Add(new WishlistItem { UserId = userId, ProductId = productId });
			await _context.SaveChangesAsync();
			return Ok(new { IsWishlisted = true, Message = "Added to wishlist" });
		}
	}
}