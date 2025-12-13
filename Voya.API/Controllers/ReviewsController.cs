using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.Application.DTOs;
using Voya.Core.Entities;
using Voya.Core.Enums; // Required for OrderStatus
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/reviews")]
public class ReviewsController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public ReviewsController(VoyaDbContext context)
	{
		_context = context;
	}

	private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")!.Value);

	// 1. Submit a Review
	[HttpPost]
	public async Task<IActionResult> AddReview(AddReviewRequest request)
	{
		var userId = GetUserId();

		// Verify purchase
		// FIX: request.OrderId is now Guid, matching o.Id
		var order = await _context.Orders
			.Include(o => o.Items)
			.FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == userId);

		if (order == null || !order.Items.Any(i => i.ProductId == request.ProductId))
			return BadRequest("You can only review products you have purchased.");

		// Check duplicate
		// FIX: IDs are now Guid comparisons
		if (await _context.Reviews.AnyAsync(r => r.UserId == userId && r.ProductId == request.ProductId && r.OrderId == request.OrderId))
			return BadRequest("You have already reviewed this item for this order.");

		var review = new Review
		{
			UserId = userId,
			ProductId = request.ProductId,
			OrderId = request.OrderId,
			Rating = request.Rating,
			Comment = request.Comment,
			ImageUrls = request.Images
		};

		_context.Reviews.Add(review);
		await _context.SaveChangesAsync();
		return Ok("Review submitted!");
	}

	// 2. Get My Reviews
	[HttpGet("mine")]
	public async Task<ActionResult<List<ReviewDto>>> GetMyReviews()
	{
		var userId = GetUserId();
		var reviews = await _context.Reviews
			.Include(r => r.User)
			.Where(r => r.UserId == userId)
			.OrderByDescending(r => r.CreatedAt)
			.Select(r => new ReviewDto(
				r.Id,
				r.User.FullName,
				r.Rating,
				r.Comment!,
				r.ImageUrls,
				r.CreatedAt.ToShortDateString()
			))
			.ToListAsync();
		return Ok(reviews);
	}

	// 3. Get Products I Bought But Didn't Review Yet
	[HttpGet("pending")]
	public async Task<ActionResult<List<PendingReviewDto>>> GetPendingReviews()
	{
		var userId = GetUserId();

		// Find delivered orders
		var deliveredOrders = await _context.Orders
			.Include(o => o.Items)
			//.ThenInclude(i => i.Product) // Optional: Include Product to avoid extra query inside loop
			.Where(o => o.UserId == userId && o.Status == OrderStatus.Delivered)
			.ToListAsync();

		var pendingList = new List<PendingReviewDto>();

		foreach (var order in deliveredOrders)
		{
			foreach (var item in order.Items)
			{
				// Check if already reviewed
				// FIX: Guid comparison now works
				bool alreadyReviewed = await _context.Reviews.AnyAsync(r =>
					r.UserId == userId &&
					r.ProductId == item.ProductId &&
					r.OrderId == order.Id);

				if (!alreadyReviewed)
				{
					// Fetch product image
					var product = await _context.Products.FindAsync(item.ProductId);

					pendingList.Add(new PendingReviewDto(
						item.ProductId,
						item.ProductName,
						product?.MainImageUrl ?? "",
						order.Id.ToString(), // FIX: Convert Guid to String for DTO
						order.PlacedAt.ToShortDateString() // FIX: Use PlacedAt instead of CreatedAt
					));
				}
			}
		}

		return Ok(pendingList);
	}
}