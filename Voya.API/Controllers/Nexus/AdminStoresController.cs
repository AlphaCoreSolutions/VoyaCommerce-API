using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.API.Attributes;
using Voya.Core.Constants;
using Voya.Core.Entities;
using Voya.Core.Enums; // Ensure StoreStatus Enum is here
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/stores")]
public class NexusStoresController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public NexusStoresController(VoyaDbContext context)
	{
		_context = context;
	}

	// --- 1. LISTING & SEARCH ---

	// GET: api/v1/nexus/stores?status=Pending
	[HttpGet]
	[RequirePermission(Permissions.StoresView)]
	public async Task<IActionResult> GetStores([FromQuery] string? status = null)
	{
		var query = _context.Stores
			.Include(s => s.Owner) // To get Owner Name
			.AsQueryable();

		// Optional Filter by Enum Status
		if (!string.IsNullOrEmpty(status) && Enum.TryParse<StoreStatus>(status, true, out var statusEnum))
		{
			query = query.Where(s => s.Status == statusEnum);
		}

		var stores = await query
			.OrderByDescending(s => s.CreatedAt)
			.Select(s => new
			{
				s.Id,
				s.Name,
				s.Description,
				Owner = s.Owner.FullName,
				Status = s.Status.ToString(),
				s.CommissionRate,
				s.IsVerified,
				s.IsBoosted,
				JoinedAt = s.CreatedAt
			})
			.ToListAsync();

		return Ok(stores);
	}

	// GET: api/v1/nexus/stores/{id}
	[HttpGet("{id}")]
	[RequirePermission(Permissions.StoresView)]
	public async Task<IActionResult> GetStoreDetails(Guid id)
	{
		var store = await _context.Stores
			.Include(s => s.Owner)
			.FirstOrDefaultAsync(s => s.Id == id);

		if (store == null) return NotFound();

		return Ok(new
		{
			store.Id,
			store.Name,
			store.Description,
			store.Status,
			store.AdminNotes,
			Owner = store.Owner.FullName,
			store.CommissionRate,
			store.IsVerified,
			store.IsBoosted
		});
	}

	// --- 2. WORKFLOW (APPROVE / REJECT) ---

	[HttpPost("{id}/decide")]
	[RequirePermission(Permissions.StoresApprove)]
	public async Task<IActionResult> DecideApplication(Guid id, [FromBody] bool approved)
	{
		var store = await _context.Stores.FindAsync(id);
		if (store == null) return NotFound();

		if (approved)
		{
			store.Status = StoreStatus.Active;
			store.ActivatedAt = DateTime.UtcNow;
			store.AdminNotes = "Approved via Nexus Console.";
		}
		else
		{
			store.Status = StoreStatus.Rejected;
			store.AdminNotes = "Rejected via Nexus Console.";
		}

		await _context.SaveChangesAsync();
		return Ok(approved ? "Store approved and live." : "Store rejected.");
	}

	// LEGACY SUPPORT: Keep your explicit status update endpoint if needed for advanced states
	[HttpPost("{id}/status")]
	[RequirePermission(Permissions.StoresManage)]
	public async Task<IActionResult> UpdateStoreStatus(Guid id, [FromBody] UpdateStatusDto request)
	{
		var store = await _context.Stores.FindAsync(id);
		if (store == null) return NotFound();

		store.Status = request.NewStatus;
		store.AdminNotes = request.Notes;

		if (request.NewStatus == StoreStatus.Active && store.ActivatedAt == null)
		{
			store.ActivatedAt = DateTime.UtcNow;
		}

		await _context.SaveChangesAsync();
		return Ok(new { Message = $"Store status updated to {request.NewStatus}" });
	}

	// --- 3. CONFIGURATION (COMMISSION, BOOST, VERIFY) ---

	[HttpPut("{id}/config")]
	[RequirePermission(Permissions.StoresManage)]
	public async Task<IActionResult> UpdateConfig(Guid id, [FromBody] StoreConfigDto config)
	{
		var store = await _context.Stores.FindAsync(id);
		if (store == null) return NotFound();

		store.IsVerified = config.IsVerified;
		store.IsBoosted = config.IsBoosted;

		// Validation: Commission shouldn't be negative or > 100
		if (config.CommissionRate >= 0 && config.CommissionRate <= 100)
		{
			store.CommissionRate = config.CommissionRate;
		}

		await _context.SaveChangesAsync();
		return Ok("Store configuration updated.");
	}
}

// --- DTOs ---

public class UpdateStatusDto
{
	public StoreStatus NewStatus { get; set; }
	public string? Notes { get; set; }
}

public class StoreConfigDto
{
	public bool IsVerified { get; set; }
	public bool IsBoosted { get; set; }
	public decimal CommissionRate { get; set; }
}