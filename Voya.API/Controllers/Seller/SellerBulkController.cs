using Microsoft.AspNetCore.Mvc;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Seller;

[ApiController]
[Route("api/v1/seller/bulk")]
public class SellerBulkController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public SellerBulkController(VoyaDbContext context) { _context = context; }

	// FEATURE 10: BULK PRICE EDIT
	[HttpPost("price")]
	public async Task<IActionResult> BulkUpdatePrice([FromBody] BulkPriceUpdateDto request)
	{
		var products = _context.Products.Where(p => request.ProductIds.Contains(p.Id));

		foreach (var p in products)
		{
			if (request.IsPercentage)
				p.BasePrice *= (1 + (request.Value / 100)); // e.g. +10%
			else
				p.BasePrice += request.Value; // e.g. +$5
		}

		await _context.SaveChangesAsync();
		return Ok("Bulk update complete.");
	}
}

public class BulkPriceUpdateDto
{
	public List<Guid> ProductIds { get; set; }
	public decimal Value { get; set; }
	public bool IsPercentage { get; set; }
}