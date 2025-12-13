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
		// 1. Get Flash Sales (Simulated by checking for DiscountPrice)
		var flashSales = await _context.Products
			.Where(p => p.DiscountPrice != null)
			.Take(5)
			.Select(p => new ProductListDto(p.Id, p.Name, p.BasePrice, p.DiscountPrice, p.MainImageUrl, 4.9))
			.ToListAsync();

		// 2. Get Highlights (Random or newest items)
		var highlights = await _context.Products
			.OrderByDescending(p => p.Id) // Simple sort by "newest" roughly
			.Take(10)
			.Select(p => new ProductListDto(p.Id, p.Name, p.BasePrice, p.DiscountPrice, p.MainImageUrl, 4.5))
			.ToListAsync();

		// 3. Static Banners for now
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
			FlashSaleEndTime: endOfToday // Sends exact sync time
		));
	}
}