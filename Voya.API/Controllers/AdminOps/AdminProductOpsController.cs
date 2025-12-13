using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.API.Attributes;
using Voya.Core.Constants;
using Voya.Core.Enums;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.AdminOps;

[Authorize]
[ApiController]
[Route("api/v1/admin/products")]
public class AdminProductOpsController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public AdminProductOpsController(VoyaDbContext context) { _context = context; }

	// 1. Get Products needing review
	[HttpGet("queue")]
	[RequirePermission(Permissions.ContentManage)]
	public async Task<IActionResult> GetReviewQueue()
	{
		var products = await _context.Products
			.Include(p => p.Category)
			.Where(p => p.ApprovalStatus == ProductApprovalStatus.PendingReview)
			.Select(p => new { p.Id, p.Name, p.MainImageUrl, p.BasePrice, StoreId = p.StoreId })
			.ToListAsync();

		return Ok(products);
	}

	// 2. Approve/Reject Product
	[HttpPost("{productId}/decide")]
	[RequirePermission(Permissions.ContentManage)]
	public async Task<IActionResult> DecideProduct(Guid productId, [FromBody] DecisionDto request)
	{
		var product = await _context.Products.FindAsync(productId);
		if (product == null) return NotFound();

		if (request.Approved)
		{
			product.ApprovalStatus = ProductApprovalStatus.Approved;
		}
		else
		{
			product.ApprovalStatus = ProductApprovalStatus.Rejected;
			product.RejectionReason = request.Reason;
		}

		await _context.SaveChangesAsync();
		return Ok("Product status updated.");
	}
}