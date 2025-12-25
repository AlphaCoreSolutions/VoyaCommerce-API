using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.API.Attributes;
using Voya.Core.Constants;
using Voya.Application.DTOs.Nexus;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/global")]
public class NexusGlobalController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public NexusGlobalController(VoyaDbContext context)
	{
		_context = context;
	}

	// GET /api/v1/nexus/global/overview?days=7
	[HttpGet("overview")]
	[RequirePermission(Permissions.GlobalView)]
	public async Task<IActionResult> Overview([FromQuery] int days = 7)
	{
		if (days < 1) days = 7;
		if (days > 90) days = 90;

		var since = DateTime.UtcNow.Date.AddDays(-days);

		var totalUsers = await _context.Users.AsNoTracking().CountAsync();
		var totalStores = await _context.Stores.AsNoTracking().CountAsync();
		var totalOrders = await _context.Orders.AsNoTracking().CountAsync();
		var totalRevenue = await _context.Orders.AsNoTracking()
			.SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

		var ordersLast = await _context.Orders.AsNoTracking()
			.CountAsync(o => o.PlacedAt >= since);

		var revenueLast = await _context.Orders.AsNoTracking()
			.Where(o => o.PlacedAt >= since)
			.SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

		var dto = new NexusGlobalOverviewDto
		{
			Totals = new NexusGlobalTotalsDto
			{
				TotalUsers = totalUsers,
				TotalStores = totalStores,
				TotalOrders = totalOrders,
				TotalRevenue = totalRevenue,
				OrdersLast7Days = ordersLast,
				RevenueLast7Days = revenueLast
			}
		};

		return Ok(dto);
	}

}
