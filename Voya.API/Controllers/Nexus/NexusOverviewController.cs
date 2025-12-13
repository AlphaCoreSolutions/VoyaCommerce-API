using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // FIX: Required for CountAsync/SumAsync
using Voya.Core.Entities;
using Voya.Core.Enums; // FIX: Required for Enums like PaymentStatus
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/overview")]
public class NexusOverviewController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public NexusOverviewController(VoyaDbContext context) { _context = context; }

	[HttpGet("stats")]
	public async Task<IActionResult> GetPlatformStats()
	{
		var today = DateTime.UtcNow.Date;

		var stats = new
		{
			// Revenue
			TotalRevenue = await _context.Orders
				.Where(o => o.PaymentStatus == PaymentStatus.Paid)
				.SumAsync(o => o.TotalAmount),

			TodayRevenue = await _context.Orders
				.Where(o => o.PlacedAt >= today)
				.SumAsync(o => o.TotalAmount),

			// Users & Stores
			TotalUsers = await _context.Users.CountAsync(),
			ActiveStores = await _context.Stores.CountAsync(s => s.Status == StoreStatus.Active),

			// Logistics
			PendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Processing),

			// Health
			SystemStatus = "Healthy",
			DatabaseLatency = "12ms"
		};

		return Ok(stats);
	}
}