using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Voya.Application.DTOs;
using Voya.Core.Entities;
using Voya.Core.Enums; // If needed
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Seller;

[Authorize]
[ApiController]
[Route("api/v1/seller/inventory")]
public class SellerInventoryController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public SellerInventoryController(VoyaDbContext context)
	{
		_context = context;
	}

	private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

	// 1. LIST MY PRODUCTS
	[HttpGet]
	public async Task<IActionResult> GetMyProducts()
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);
		if (store == null) return Unauthorized("No store found.");

		var products = await _context.Products
			.Where(p => p.StoreId == store.Id)
			.Select(p => new
			{
				p.Id,
				p.Name,
				p.BasePrice,
				p.StockQuantity,
				p.MainImageUrl,
				Sales = 0 // Placeholder
			})
			.ToListAsync();

		return Ok(products);
	}

	// 2. ADD NEW PRODUCT
	[HttpPost]
	public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto request)
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);

		if (store == null) return Unauthorized("Create a store first.");
		if (store.Status != StoreStatus.Active) return BadRequest("Store is not active.");

		var product = new Product
		{
			StoreId = store.Id,
			CategoryId = request.CategoryId,
			Name = request.Name,
			Description = request.Description,
			BasePrice = request.BasePrice,
			DiscountPrice = request.DiscountPrice,
			StockQuantity = request.StockQuantity,
			MainImageUrl = request.MainImageUrl,
			GalleryImages = request.GalleryImages,
			Tags = request.Tags,
			CreatedAt = DateTime.UtcNow // Ensure Product entity has CreatedAt if not already
		};

		// Handle Options
		if (request.Options != null)
		{
			foreach (var opt in request.Options)
			{
				var productOption = new ProductOption
				{
					Name = opt.Name,
					Values = opt.Values.Select(v => new ProductOptionValue
					{
						Id = Guid.NewGuid().ToString().Substring(0, 8), // Simple ID
						Label = v.Label,
						PriceModifier = v.PriceModifier
					}).ToList()
				};
				product.Options.Add(productOption);
			}
		}

		_context.Products.Add(product);
		await _context.SaveChangesAsync();

		return Ok(new { Message = "Product created", ProductId = product.Id });
	}

	// 3. DELETE PRODUCT
	[HttpDelete("{id}")]
	public async Task<IActionResult> DeleteProduct(Guid id)
	{
		var userId = GetUserId();
		var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == userId);
		if (store == null) return Unauthorized();

		var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && p.StoreId == store.Id);
		if (product == null) return NotFound("Product not found or access denied.");

		_context.Products.Remove(product);
		await _context.SaveChangesAsync();

		return Ok("Product deleted.");
	}
}