using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.API.Attributes;
using Voya.Core.Constants;
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
	// Security: requires SystemConfig because it can expose sensitive objects.
	[HttpGet("search/global")]
	[RequirePermission(Permissions.SystemConfig)]
	public async Task<IActionResult> GlobalSearch([FromQuery] string query, [FromQuery] int take = 5)
	{
		query = (query ?? "").Trim();

		// Security tightening: block empty / too-short searches
		if (query.Length < 3)
			return BadRequest("Query must be at least 3 characters.");

		// Hard cap to prevent data scraping
		if (take < 1) take = 5;
		if (take > 10) take = 10;

		// NOTE: do NOT return full entities from admin search (PII leakage risk).
		// Return a minimal projection for each type.
		var users = await _context.Users
			.AsNoTracking()
			.Where(u =>
				(!string.IsNullOrEmpty(u.Email) && u.Email.Contains(query)) ||
				(!string.IsNullOrEmpty(u.FullName) && u.FullName.Contains(query)))
			.OrderByDescending(u => u.CreatedAt)
			.Take(take)
			.Select(u => new
			{
				u.Id,
				u.FullName,
				u.Email,
				u.CreatedAt,
				IsActive = u.IsActive,
				IsBanned = u.IsBanned,
				IsStaff = u.NexusRoleId != null,
				u.TrustScore
			})
			.ToListAsync();


		var stores = await _context.Stores
			.AsNoTracking()
			.Where(s => !string.IsNullOrEmpty(s.Name) && s.Name.Contains(query))
			.OrderByDescending(s => s.CreatedAt)
			.Take(take)
			.Select(s => new
			{
				s.Id,
				s.Name,
				s.Status,
				s.CreatedAt
			})
			.ToListAsync();

		var products = await _context.Products
			.AsNoTracking()
			.Where(p => !string.IsNullOrEmpty(p.Name) && p.Name.Contains(query))
			.OrderByDescending(p => p.CreatedAt)
			.Take(take)
			.Select(p => new
			{
				p.Id,
				p.Name,
				p.BasePrice,
				p.StockQuantity,
				p.ApprovalStatus,
				p.CreatedAt
			})
			.ToListAsync();

		// Orders: searching by ID string is okay, but still cap.
		var orders = await _context.Orders
			.AsNoTracking()
			.Where(o => o.Id.ToString().Contains(query))
			.OrderByDescending(o => o.PlacedAt)
			.Take(take)
			.Select(o => new
			{
				o.Id,
				o.Status,
				o.TotalAmount,
				o.PlacedAt,
				o.UserId
			})
			.ToListAsync();

		return Ok(new
		{
			Query = query,
			Take = take,
			Users = users,
			Stores = stores,
			Products = products,
			Orders = orders
		});
	}

	// FEATURE 2/10: USER DEVICE MAP / PLATFORM HEATMAP
	[HttpGet("geo-data")]
	[RequirePermission(Permissions.SystemConfig)]
	public async Task<IActionResult> GetGeoData([FromQuery] int take = 100)
	{
		// Security tightening: cap output size
		if (take < 1) take = 100;
		if (take > 500) take = 500;

		// Group by City to avoid exposing raw addresses
		var cityCounts = await _context.Addresses
			.AsNoTracking()
			.Where(a => !string.IsNullOrEmpty(a.City))
			.GroupBy(a => a.City!)
			.Select(g => new
			{
				City = g.Key,
				UserCount = g.Count()
			})
			.OrderByDescending(x => x.UserCount)
			.Take(take)
			.ToListAsync();

		return Ok(new
		{
			Take = take,
			Items = cityCounts
		});
	}

	// FEATURE 4: SAFE REPORTS (pre-built)
	[HttpGet("reports/custom")]
	[RequirePermission(Permissions.SystemConfig)]
	public async Task<IActionResult> GetSafeReport([FromQuery] string reportType, [FromQuery] int take = 200)
	{
		reportType = (reportType ?? "").Trim();

		if (string.IsNullOrWhiteSpace(reportType))
			return BadRequest("reportType is required.");

		if (take < 1) take = 200;
		if (take > 1000) take = 1000;

		// Normalize
		var rt = reportType.ToLowerInvariant();

		if (rt == "highvalueusers")
		{
			var data = await _context.Users
				.AsNoTracking()
				.Where(u => u.TotalSpentLifetime > 1000)
				.OrderByDescending(u => u.TotalSpentLifetime)
				.Take(take)
				.Select(u => new
				{
					u.Id,
					u.FullName,
					u.Email,
					u.TotalSpentLifetime
				})
				.ToListAsync();

			return Ok(new
			{
				ReportType = "HighValueUsers",
				Take = take,
				Items = data
			});
		}

		// Add more safe report types here...
		return BadRequest("Unknown report type.");
	}

	// FEATURE 5: RECYCLE BIN
	[HttpGet("recycle-bin")]
	[RequirePermission(Permissions.SystemConfig)]
	public async Task<IActionResult> GetDeletedItems([FromQuery] int take = 50)
	{
		if (take < 1) take = 50;
		if (take > 200) take = 200;

		// Security: do not return full JsonData by default (can contain secrets/PII).
		var items = await _context.RecycleBinItems
			.AsNoTracking()
			.OrderByDescending(d => d.DeletedAt)
			.Take(take)
			.Select(d => new
			{
				d.Id,
				d.EntityType,
				d.OriginalId,
				d.DeletedAt,
				d.DeletedByUserId
			})
			.ToListAsync();

		return Ok(new { Take = take, Items = items });
	}

	// Restore is still mock: it just removes the recycle bin entry.
	// Later we can implement actual restore per EntityType.
	[HttpPost("recycle-bin/{id:guid}/restore")]
	[RequirePermission(Permissions.SystemConfig)]
	public async Task<IActionResult> RestoreItem(Guid id)
	{
		var item = await _context.RecycleBinItems.FirstOrDefaultAsync(x => x.Id == id);
		if (item == null) return NotFound();

		// SECURITY NOTE:
		// We are NOT restoring the original entity yet. We only remove the recycle-bin marker.
		// This prevents a false claim of "restore succeeded".
		_context.RecycleBinItems.Remove(item);
		await _context.SaveChangesAsync();

		return Ok(new
		{
			Message = "Recycle bin entry removed (restore is currently mock).",
			item.Id,
			item.EntityType,
			item.OriginalId
		});
	}
}
