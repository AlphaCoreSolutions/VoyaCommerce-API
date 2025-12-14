using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.Application.DTOs;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers;

[ApiController]
[Route("api/v1/home")]
public class HomeController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public HomeController(VoyaDbContext context)
	{
		_context = context;
	}

	[AllowAnonymous]
	[HttpGet("feed")]
	public async Task<ActionResult<HomeFeedDto>> GetFeed()
	{
		// 1. Get Flash Sales
		var flashSales = await _context.Products
			.Include(p => p.Store) // <--- CRITICAL: Load Store data
			.Where(p => p.DiscountPrice != null)
			.Take(5)
			.Select(p => new ProductListDto(
				p.Id,
				p.Name,
				p.BasePrice,
				p.DiscountPrice,
				p.MainImageUrl,
				4.9,

				// --- NEW FIELDS MAPPED ---
				p.StoreId,
				p.Store != null ? p.Store.Name : "Voya Store",
				p.Store != null ? (p.Store.LogoUrl ?? "") : "",
				p.Store != null ? p.Store.Rating : 5.0
			))
			.ToListAsync();

		// 2. Get Highlights
		var highlights = await _context.Products
			.Include(p => p.Store) // <--- CRITICAL: Load Store data
			.OrderByDescending(p => p.Id)
			.Take(10)
			.Select(p => new ProductListDto(
				p.Id,
				p.Name,
				p.BasePrice,
				p.DiscountPrice,
				p.MainImageUrl,
				4.5,

				// --- NEW FIELDS MAPPED ---
				p.StoreId,
				p.Store != null ? p.Store.Name : "Voya Store",
				p.Store != null ? (p.Store.LogoUrl ?? "") : "",
				p.Store != null ? p.Store.Rating : 5.0
			))
			.ToListAsync();

		// 3. Static Banners
		var banners = new List<string>
		{
			"https://placehold.co/600x200/orange/white?text=Summer+Sale",
			"https://placehold.co/600x200/purple/white?text=New+Arrivals"
		};

		var today = DateTime.UtcNow.Date;
		var endOfToday = today.AddDays(1).AddTicks(-1);

		return Ok(new HomeFeedDto(
			flashSales,
			highlights,
			banners,
			FlashSaleEndTime: endOfToday
		));
	}
}