using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/tools")]
public class NexusToolsController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public NexusToolsController(VoyaDbContext context) { _context = context; }

	// FEATURE 1: GOD MODE SEARCH
	[HttpGet("search/global")]
	public async Task<IActionResult> GlobalSearch([FromQuery] string query)
	{
		// Parallel search across users, stores, and products
		var users = await _context.Users.Where(u => u.Email.Contains(query) || u.FullName.Contains(query)).Take(5).ToListAsync();
		var stores = await _context.Stores.Where(s => s.Name.Contains(query)).Take(5).ToListAsync();
		var products = await _context.Products.Where(p => p.Name.Contains(query)).Take(5).ToListAsync();
		var orders = await _context.Orders.Where(o => o.Id.ToString().Contains(query)).Take(5).ToListAsync();

		return Ok(new { Users = users, Stores = stores, Products = products, Orders = orders });
	}

	// FEATURE 2: USER DEVICE MAP (Heatmap Data)
	// FEATURE 10: PLATFORM HEATMAP (REAL DB AGGREGATION)
	[HttpGet("geo-data")]
	public async Task<IActionResult> GetGeoData()
	{
		// Since we might not have Lat/Lng for every address, 
		// we group by City/Governorate name to get counts.
		var cityCounts = await _context.Addresses
			.Where(a => !string.IsNullOrEmpty(a.City))
			.GroupBy(a => a.City)
			.Select(g => new
			{
				City = g.Key,
				UserCount = g.Count()
			})
			.OrderByDescending(x => x.UserCount)
			.ToListAsync();

		// If you have Lat/Lng in Address entity, use this instead:
		/*
        var heatPoints = await _context.Addresses
            .Where(a => a.Latitude != 0)
            .Select(a => new { Lat = a.Latitude, Lng = a.Longitude, Weight = 1 })
            .ToListAsync();
        */

		return Ok(cityCounts);
	}
	// FEATURE 4 (SAFE REPLACEMENT): DATA EXPLORER
	// Instead of raw SQL, we provide pre-built safe reports
	[HttpGet("reports/custom")]
	public async Task<IActionResult> GetSafeReport([FromQuery] string reportType)
	{
		if (reportType == "HighValueUsers")
		{
			var data = await _context.Users
				.Where(u => u.TotalSpentLifetime > 1000)
				.Select(u => new { u.Id, u.FullName, u.Email, u.TotalSpentLifetime })
				.ToListAsync();
			return Ok(data);
		}
		// Add more safe report types...
		return BadRequest("Unknown report type.");
	}

	// FEATURE 5: RECYCLE BIN
	[HttpGet("recycle-bin")]
	public async Task<IActionResult> GetDeletedItems()
	{
		return Ok(await _context.RecycleBinItems.OrderByDescending(d => d.DeletedAt).Take(50).ToListAsync());
	}

	[HttpPost("recycle-bin/{id}/restore")]
	public async Task<IActionResult> RestoreItem(Guid id)
	{
		var item = await _context.RecycleBinItems.FindAsync(id);
		if (item == null) return NotFound();

		// Logic: Deserialize item.JsonData and re-insert into table
		// Implementation requires generic deserialization or specific handling per type.
		// For mockup:
		_context.RecycleBinItems.Remove(item);
		await _context.SaveChangesAsync();

		return Ok($"Item {item.OriginalId} restored (mock).");
	}
}