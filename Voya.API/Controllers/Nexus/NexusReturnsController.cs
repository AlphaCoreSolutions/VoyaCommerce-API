using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.API.Attributes;
using Voya.Core.Constants;
using Voya.Core.Enums;
using Voya.Infrastructure.Persistence;
using Voya.Application.DTOs.Nexus;
using Voya.Core.Entities;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/returns")]
public class NexusReturnsController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public NexusReturnsController(VoyaDbContext context)
	{
		_context = context;
	}

	// GET /api/v1/nexus/returns
	[HttpGet]
	[RequirePermission(Permissions.ReturnsView)]
	public async Task<IActionResult> GetReturns(
		[FromQuery] string? status,
		[FromQuery] string? q,
		[FromQuery] DateTime? dateFrom,
		[FromQuery] DateTime? dateTo,
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 20)
	{
		if (page < 1) page = 1;
		if (pageSize < 1) pageSize = 20;
		if (pageSize > 200) pageSize = 200;

		var query = _context.ReturnRequests
			.AsNoTracking()
			.Include(r => r.User)
			.Include(r => r.Order)
			.OrderByDescending(r => r.CreatedAt)
			.AsQueryable();

		if (!string.IsNullOrWhiteSpace(status))
		{
			if (!Enum.TryParse<ReturnStatus>(status, true, out var parsed))
				return BadRequest("Invalid status.");

			query = query.Where(r => r.Status == parsed);
		}

		if (dateFrom.HasValue)
			query = query.Where(r => r.CreatedAt >= dateFrom.Value);

		if (dateTo.HasValue)
			query = query.Where(r => r.CreatedAt <= dateTo.Value);

		if (!string.IsNullOrWhiteSpace(q))
		{
			q = q.Trim();
			query = query.Where(r =>
				r.Id.ToString().Contains(q) ||
				r.OrderId.ToString().Contains(q) ||
				r.User.Email.Contains(q) ||
				r.User.FullName.Contains(q)
			);
		}

		var total = await query.CountAsync();

		var items = await query
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.Select(r => new NexusReturnListItemDto
			{
				Id = r.Id,
				Status = r.Status.ToString(),

				OrderId = r.OrderId,
				OrderTotal = r.Order.TotalAmount,
				RequestedAt = r.CreatedAt,

				UserId = r.UserId,
				UserName = r.User.FullName,
				UserEmail = r.User.Email,

				Reason = r.Reason,
				Restock = r.Restock
			})
			.ToListAsync();

		return Ok(new NexusPagedResult<NexusReturnListItemDto>
		{
			Items = items,
			Page = page,
			PageSize = pageSize,
			TotalCount = total
		});
	}

	// GET /api/v1/nexus/returns/{id}
	[HttpGet("{id:guid}")]
	[RequirePermission(Permissions.ReturnsView)]
	public async Task<IActionResult> GetReturn(Guid id)
	{
		var r = await _context.ReturnRequests
			.AsNoTracking()
			.Include(x => x.User)
			.Include(x => x.Order)
			.FirstOrDefaultAsync(x => x.Id == id);

		if (r == null) return NotFound();

		return Ok(new NexusReturnDetailDto
		{
			Id = r.Id,
			Status = r.Status.ToString(),

			OrderId = r.OrderId,
			RequestedAt = r.CreatedAt,

			UserId = r.UserId,
			UserName = r.User.FullName,
			UserEmail = r.User.Email,

			Reason = r.Reason,
			EvidenceUrlsJson = r.EvidenceUrlsJson,

			InspectionNote = r.InspectionNote,
			Restock = r.Restock,
			RejectionReason = r.RejectionReason
		});
	}

	// POST /api/v1/nexus/returns/{id}/inspect
	[HttpPost("{id:guid}/inspect")]
	[RequirePermission(Permissions.ReturnsManage)]
	public async Task<IActionResult> Inspect(Guid id, [FromBody] NexusInspectReturnRequest body)
	{
		var r = await _context.ReturnRequests.FirstOrDefaultAsync(x => x.Id == id);
		if (r == null) return NotFound();

		if (r.Status == ReturnStatus.Approved || r.Status == ReturnStatus.Rejected)
			return BadRequest("Return is already finalized.");

		r.Status = ReturnStatus.UnderInspection;
		r.InspectionNote = body.InspectionNote ?? "";
		r.Restock = body.Restock;
		r.UpdatedAt = DateTime.UtcNow;

		await _context.SaveChangesAsync();
		return Ok(new { r.Id, Status = r.Status.ToString() });
	}

	// POST /api/v1/nexus/returns/{id}/approve
	[HttpPost("{id:guid}/approve")]
	[RequirePermission(Permissions.ReturnsManage)]
	public async Task<IActionResult> Approve(Guid id)
	{
		var r = await _context.ReturnRequests
			.Include(x => x.Order)
			.FirstOrDefaultAsync(x => x.Id == id);

		if (r == null) return NotFound();
		if (r.Status == ReturnStatus.Approved) return Ok(new { r.Id, Status = r.Status.ToString() });

		r.Status = ReturnStatus.Approved;
		r.RejectionReason = null;
		r.UpdatedAt = DateTime.UtcNow;

		// Optional next steps you can add later:
		// - if Restock == true: increase product quantity / inventory
		// - initiate refund flow
		// - create audit log entry

		await _context.SaveChangesAsync();
		return Ok(new { r.Id, Status = r.Status.ToString(), r.Restock });
	}

	// POST /api/v1/nexus/returns/{id}/reject
	[HttpPost("{id:guid}/reject")]
	[RequirePermission(Permissions.ReturnsManage)]
	public async Task<IActionResult> Reject(Guid id, [FromBody] string? rejectionReason)
	{
		var r = await _context.ReturnRequests.FirstOrDefaultAsync(x => x.Id == id);
		if (r == null) return NotFound();

		r.Status = ReturnStatus.Rejected;
		r.RejectionReason = rejectionReason ?? "Rejected by admin.";
		r.UpdatedAt = DateTime.UtcNow;

		await _context.SaveChangesAsync();
		return Ok(new { r.Id, Status = r.Status.ToString(), r.RejectionReason });
	}
}
