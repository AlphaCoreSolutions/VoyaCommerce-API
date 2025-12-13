using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Linq.Dynamic.Core; // Requires: dotnet add package System.Linq.Dynamic.Core
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/analytics")]
public class AnalyticsController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public AnalyticsController(VoyaDbContext context)
	{
		_context = context;
	}

	// Request Model
	public record AdvancedSearchRequest(
		string TableName,  // "orders", "users", "products"
		string FilterQuery, // "TotalAmount > 50 AND Status == \"Pending\""
		string SortBy = "Id",
		int Limit = 100
	);

	[HttpPost("advanced-search")]
	public async Task<IActionResult> RunDynamicQuery(AdvancedSearchRequest request)
	{
		// NOTE: This uses System.Linq.Dynamic.Core for safety.
		// It allows string-based LINQ without exposing raw SQL injection.

		try
		{
			if (request.TableName.ToLower() == "orders")
			{
				var query = _context.Orders.AsQueryable();
				if (!string.IsNullOrEmpty(request.FilterQuery))
					query = query.Where(request.FilterQuery); // Dynamic Where

				var results = await query.OrderBy(request.SortBy).Take(request.Limit).ToListAsync();
				return Ok(results);
			}

			if (request.TableName.ToLower() == "users")
			{
				var query = _context.Users.AsQueryable();
				if (!string.IsNullOrEmpty(request.FilterQuery))
					query = query.Where(request.FilterQuery);

				var results = await query.OrderBy(request.SortBy).Take(request.Limit).ToListAsync();
				return Ok(results);
			}

			if (request.TableName.ToLower() == "products")
			{
				var query = _context.Products.AsQueryable();
				if (!string.IsNullOrEmpty(request.FilterQuery))
					query = query.Where(request.FilterQuery);

				var results = await query.OrderBy(request.SortBy).Take(request.Limit).ToListAsync();
				return Ok(results);
			}

			return BadRequest($"Table '{request.TableName}' not supported for analytics.");
		}
		catch (Exception ex)
		{
			return BadRequest($"Invalid Query Syntax: {ex.Message}");
		}
	}
}