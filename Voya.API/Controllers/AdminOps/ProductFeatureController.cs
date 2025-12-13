using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers;

[ApiController]
[Route("api/v1/products/features")]
public class ProductFeatureController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public ProductFeatureController(VoyaDbContext context) { _context = context; }

	// FEATURE 9: DYNAMIC FILTERS (Facets)
	[HttpGet("filters/{categoryId}")]
	public async Task<IActionResult> GetFiltersForCategory(Guid categoryId)
	{
		// Logic: Look at all products in this category, find the "Options" (e.g. Color, Size)
		// and return the unique values so the frontend can build a filter sidebar.

		var options = await _context.ProductOptions
			.Include(o => o.Values)
			.Where(o => _context.Products.Any(p => p.Id == o.ProductId && p.CategoryId == categoryId))
			.GroupBy(o => o.Name)
			.Select(g => new
			{
				FilterName = g.Key, // e.g. "Color"
				PossibleValues = g.SelectMany(x => x.Values).Select(v => v.Label).Distinct().ToList() // ["Red", "Blue"]
			})
			.ToListAsync();

		return Ok(options);
	}
}