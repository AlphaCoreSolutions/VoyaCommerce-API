using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Seller;

[Authorize]
[ApiController]
[Route("api/v1/seller/logistics")]
public class SellerLogisticsController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public SellerLogisticsController(VoyaDbContext context) { _context = context; }
	private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

	// --- PICKUP SCHEDULING ---
	[HttpPost("pickup/settings")]
	public async Task<IActionResult> UpdatePickupSettings([FromBody] StorePickupSettings settings)
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);

		var existing = await _context.PickupSettings.FirstOrDefaultAsync(p => p.StoreId == store!.Id);
		if (existing == null)
		{
			settings.StoreId = store!.Id;
			_context.PickupSettings.Add(settings);
		}
		else
		{
			existing.IsPickupEnabled = settings.IsPickupEnabled;
			existing.Instructions = settings.Instructions;
		}
		await _context.SaveChangesAsync();
		return Ok("Pickup settings updated.");
	}

	// --- DYNAMIC DELIVERY CALCULATOR ---
	[HttpGet("delivery-fee/calculate")]
	public async Task<IActionResult> CalculateDeliveryFee(double userLat, double userLng)
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);

		// 1. Get Store/Warehouse Location (Mocked for now, usually stored in Warehouse entity)
		double storeLat = 31.9539; // Amman
		double storeLng = 35.9106;

		// 2. Haversine Formula for Distance (Km)
		var distanceKm = GetDistance(storeLat, storeLng, userLat, userLng);

		// 3. Calculate Fee
		var fee = store.DeliveryBaseFee + (store.DeliveryFeePerKm * (decimal)distanceKm);

		return Ok(new
		{
			DistanceKm = Math.Round(distanceKm, 2),
			EstimatedFee = Math.Round(fee, 2)
		});
	}

	// --- GIFT WRAP MANAGEMENT ---

	[HttpGet("gift-wraps")]
	public async Task<IActionResult> GetGiftWraps()
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);

		return Ok(await _context.GiftWrapOptions
			.Where(g => g.StoreId == store!.Id)
			.ToListAsync());
	}

	[HttpPost("gift-wraps")]
	public async Task<IActionResult> AddGiftWrap([FromBody] GiftWrapOption option)
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);

		option.StoreId = store!.Id;
		_context.GiftWrapOptions.Add(option);
		await _context.SaveChangesAsync();
		return Ok(option);
	}

	// Helper: Haversine Formula
	private double GetDistance(double lat1, double lon1, double lat2, double lon2)
	{
		var R = 6371; // Radius of earth in km
		var dLat = ToRad(lat2 - lat1);
		var dLon = ToRad(lon2 - lon1);
		var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
				Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
				Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
		var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
		return R * c;
	}
	private double ToRad(double deg) => deg * (Math.PI / 180);
}