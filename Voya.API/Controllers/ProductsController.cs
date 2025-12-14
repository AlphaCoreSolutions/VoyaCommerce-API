using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.Application.DTOs;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers;

[ApiController]
[Route("api/v1/products")]
public class ProductsController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public ProductsController(VoyaDbContext context)
	{
		_context = context;
	}

	// 1. PUBLIC SEARCH & FILTER
	[AllowAnonymous]
	[HttpGet]
	public async Task<ActionResult<List<ProductListDto>>> GetProducts(
		[FromQuery] string? search,
		[FromQuery] Guid? categoryId,
		[FromQuery] int page = 1,
		[FromQuery] int limit = 20)
	{
		var query = _context.Products
			.Include(p => p.Store) // <--- CRITICAL: Fetch Store Data
			.AsQueryable();

		if (!string.IsNullOrEmpty(search))
		{
			var term = search.ToLower().Trim();
			// Note: .Any() on Tags only works if configured correctly in DbContext or via value conversion
			query = query.Where(p =>
				p.Name.ToLower().Contains(term) ||
				p.Description.ToLower().Contains(term)
			);
		}

		if (categoryId.HasValue)
		{
			query = query.Where(p => p.CategoryId == categoryId);
		}

		var products = await query
			.Skip((page - 1) * limit)
			.Take(limit)
			.Select(p => new ProductListDto(
				p.Id,
				p.Name,
				p.BasePrice,
				p.DiscountPrice,
				p.MainImageUrl,
				4.8, // Product Rating (Placeholder until Review system is fully linked)

				// --- MAPPED STORE DATA ---
				p.StoreId,
				p.Store != null ? p.Store.Name : "Voya Store",
				p.Store != null ? (p.Store.LogoUrl ?? "") : "",
				p.Store != null ? p.Store.Rating : 5.0
			))
			.ToListAsync();

		return Ok(products);
	}

	// 2. PRODUCT DETAILS
	[AllowAnonymous]
	[HttpGet("{id}")]
	public async Task<ActionResult<ProductDto>> GetProductDetails(Guid id)
	{
		var product = await _context.Products
			.Include(p => p.Category)
			.Include(p => p.Store) // <--- LOAD STORE
			.Include(p => p.Options)
				.ThenInclude(o => o.Values) // <--- LOAD OPTIONS
			.FirstOrDefaultAsync(p => p.Id == id);

		if (product == null) return NotFound();

		var dto = new ProductDto(
			product.Id,
			product.Name,
			product.Description,
			product.BasePrice,
			product.DiscountPrice,
			product.StockQuantity,
			product.MainImageUrl,
			product.GalleryImages,
			product.Category.Name,
			product.Options.Select(o => new ProductOptionDto(
				o.Name,
				o.Values.Select(v => new ProductOptionValueDto(v.Label, v.PriceModifier)).ToList()
			)).ToList(),
			product.Tags,

			// --- MAP STORE INFO ---
			product.StoreId,
			product.Store?.Name ?? "Voya Store",
			product.Store?.LogoUrl ?? "",
			product.Store?.Rating ?? 5.0
		);

		return Ok(dto);
	}

	// 3. VISUAL SEARCH (AI MOCK)
	[AllowAnonymous]
	[HttpPost("visual-search")]
	public async Task<IActionResult> VisualSearch(IFormFile image)
	{
		// Mock AI Response
		var aiDetectedLabels = new List<string> { "Sneaker", "Running", "Footwear" };

		// Search locally
		// We include Store here too so the result cards look correct
		var allProducts = await _context.Products
			.Include(p => p.Store)
			.Take(100)
			.ToListAsync();

		var results = allProducts
			.Where(p =>
				aiDetectedLabels.Any(label => p.Tags.Any(t => t.Contains(label, StringComparison.OrdinalIgnoreCase))) ||
				aiDetectedLabels.Any(label => p.Name.Contains(label, StringComparison.OrdinalIgnoreCase))
			)
			.Take(10)
			.Select(p => new ProductListDto(
				p.Id,
				p.Name,
				p.BasePrice,
				p.DiscountPrice,
				p.MainImageUrl,
				4.5,

				// Mapped Store Data
				p.StoreId,
				p.Store?.Name ?? "Voya Store",
				p.Store?.LogoUrl ?? "",
				p.Store?.Rating ?? 5.0
			))
			.ToList();

		return Ok(new { DetectedTags = aiDetectedLabels, Products = results });
	}

	// 4. RELATED PRODUCTS
	[AllowAnonymous]
	[HttpGet("{id}/related")]
	public async Task<ActionResult<List<ProductListDto>>> GetRelatedProducts(Guid id)
	{
		var originalProduct = await _context.Products.FindAsync(id);
		if (originalProduct == null) return NotFound();

		// Helper query to map DTO consistently
		IQueryable<ProductListDto> MapToDto(IQueryable<Product> q)
		{
			return q.Select(p => new ProductListDto(
				p.Id, p.Name, p.BasePrice, p.DiscountPrice, p.MainImageUrl, 4.5,
				p.StoreId,
				p.Store != null ? p.Store.Name : "Voya Store",
				p.Store != null ? (p.Store.LogoUrl ?? "") : "",
				p.Store != null ? p.Store.Rating : 5.0
			));
		}

		var related = await MapToDto(
				_context.Products
				.Include(p => p.Store)
				.Where(p => p.CategoryId == originalProduct.CategoryId && p.Id != id)
				.OrderBy(r => Guid.NewGuid()) // Random order
				.Take(4)
			).ToListAsync();

		if (!related.Any())
		{
			related = await MapToDto(
				_context.Products
				.Include(p => p.Store)
				.Where(p => p.Id != id)
				.Take(4)
			).ToListAsync();
		}

		return Ok(related);
	}

	// 5. DYNAMIC PRICING
	[HttpGet("{id}/dynamic-price")]
	public async Task<IActionResult> GetDynamicPrice(Guid id, string userCity)
	{
		var product = await _context.Products.FindAsync(id);
		if (product == null) return NotFound();

		decimal multiplier = 1.0m;
		// Example rule: Items are 5% more expensive in Aqaba due to logistics/taxes (mock logic)
		if (userCity?.ToLower() == "aqaba") multiplier = 1.05m;

		return Ok(new
		{
			ProductId = id,
			OriginalPrice = product.BasePrice,
			LocalPrice = product.BasePrice * multiplier,
			City = userCity
		});
	}
}