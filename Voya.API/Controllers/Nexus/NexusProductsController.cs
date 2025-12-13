using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.API.Attributes;
using Voya.Core.Constants;
using Voya.Core.Entities;
using Voya.Core.Enums; // Required for ProductApprovalStatus
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/products")]
public class NexusProductsController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public NexusProductsController(VoyaDbContext context) { _context = context; }

	// --- 1. INVENTORY MANAGEMENT ---

	// GET: api/v1/nexus/products?search=nike&lowStock=true
	[HttpGet]
	[RequirePermission(Permissions.ContentManage)]
	public async Task<IActionResult> GetProducts(
		[FromQuery] string? search = null,
		[FromQuery] bool lowStock = false,
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 20)
	{
		var query = _context.Products.AsQueryable();

		if (!string.IsNullOrEmpty(search))
			query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));

		if (lowStock)
			query = query.Where(p => p.StockQuantity < 10);

		var total = await query.CountAsync();
		var items = await query
			.OrderByDescending(p => p.CreatedAt)
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.Select(p => new
			{
				p.Id,
				p.Name,
				Price = p.BasePrice, // Map BasePrice to Price for frontend
				p.StockQuantity,
				ImageUrl = p.MainImageUrl, // Map MainImageUrl to ImageUrl
				p.IsActive,
				Status = p.ApprovalStatus.ToString()
			})
			.ToListAsync();

		return Ok(new { Total = total, Data = items });
	}

	// POST: Create Product
	[HttpPost]
	[RequirePermission(Permissions.ContentManage)]
	public async Task<IActionResult> CreateProduct([FromBody] Product product)
	{
		if (string.IsNullOrEmpty(product.Name)) return BadRequest("Name required");

		// Ensure Defaults
		product.Id = Guid.NewGuid();
		product.CreatedAt = DateTime.UtcNow;
		product.ApprovalStatus = ProductApprovalStatus.PendingReview; // Default to pending
		product.IsActive = true;

		// Initialize lists if null to avoid DB errors
		product.Tags ??= new List<string>();
		product.GalleryImages ??= new List<string>();
		product.Options ??= new List<ProductOption>();

		_context.Products.Add(product);
		await _context.SaveChangesAsync();

		return Ok(product);
	}

	// PUT: Update Product
	[HttpPut("{id}")]
	[RequirePermission(Permissions.ContentManage)]
	public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] Product product)
	{
		var existing = await _context.Products
			.Include(p => p.Options) // Include Options for updating
			.FirstOrDefaultAsync(p => p.Id == id);

		if (existing == null) return NotFound();

		// Update Fields
		existing.Name = product.Name;
		existing.Description = product.Description;
		existing.BasePrice = product.BasePrice;
		existing.DiscountPrice = product.DiscountPrice;
		existing.StockQuantity = product.StockQuantity;
		existing.MainImageUrl = product.MainImageUrl;
		existing.Tags = product.Tags;
		existing.CategoryId = product.CategoryId;

		// Handle Options (Simple Replace for now)
		existing.Options = product.Options;

		await _context.SaveChangesAsync();
		return Ok(existing);
	}

	// DELETE: Remove Product
	[HttpDelete("{id}")]
	[RequirePermission(Permissions.ContentManage)]
	public async Task<IActionResult> DeleteProduct(Guid id)
	{
		var product = await _context.Products.FindAsync(id);
		if (product == null) return NotFound();

		_context.Products.Remove(product);
		await _context.SaveChangesAsync();
		return Ok("Product deleted.");
	}

	// --- 2. MODERATION FLOW ---

	// GET: api/v1/nexus/products/pending
	[HttpGet("pending")]
	[RequirePermission(Permissions.ContentManage)]
	public async Task<IActionResult> GetPendingProducts()
	{
		// Fetch products waiting for approval
		// Note: You need to join with Store entity if you want Store Name
		// Assuming Store entity exists and has Name property.

		var products = await _context.Products
			// .Include(p => p.Store) // Uncomment if Store entity is linked
			.Where(p => p.ApprovalStatus == ProductApprovalStatus.PendingReview)
			.OrderBy(p => p.CreatedAt)
			.Select(p => new
			{
				p.Id,
				p.Name,
				p.Description,
				Price = p.BasePrice,
				ImageUrl = p.MainImageUrl,
				StoreName = "Vendor #" + p.StoreId.ToString().Substring(0, 4), // Mock or p.Store.Name
				SubmittedAt = p.CreatedAt
			})
			.ToListAsync();

		return Ok(products);
	}

	// POST: api/v1/nexus/products/{id}/moderation
	[HttpPost("{id}/moderation")]
	[RequirePermission(Permissions.ContentManage)]
	public async Task<IActionResult> ModerateProduct(Guid id, [FromBody] ModerationDecisionDto request)
	{
		var product = await _context.Products.FindAsync(id);
		if (product == null) return NotFound();

		if (request.Approved)
		{
			product.ApprovalStatus = ProductApprovalStatus.Active;
			product.IsActive = true;
			product.RejectionReason = null;
		}
		else
		{
			product.ApprovalStatus = ProductApprovalStatus.Rejected;
			product.IsActive = false;
			product.RejectionReason = request.Reason;
		}

		await _context.SaveChangesAsync();

		return Ok(new { Message = request.Approved ? "Product Approved" : "Product Rejected" });
	}

	public class ModerationDecisionDto
	{
		public bool Approved { get; set; }
		public string? Reason { get; set; }
	}
}