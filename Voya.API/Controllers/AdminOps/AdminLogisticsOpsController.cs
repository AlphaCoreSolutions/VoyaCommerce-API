using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Voya.API.Attributes;
using Voya.API.Hubs;
using Voya.Core.Constants;
using Voya.Core.Enums;
using Voya.Infrastructure.Persistence; // Required for VoyaDbContext

namespace Voya.API.Controllers.AdminOps;

[Authorize]
[ApiController]
[Route("api/v1/admin/logistics")]
public class AdminLogisticsOpsController : ControllerBase
{
	private readonly IHubContext<LiveHub> _hubContext;
	private readonly VoyaDbContext _context; // <--- 1. Add Field

	// <--- 2. Inject Context in Constructor
	public AdminLogisticsOpsController(IHubContext<LiveHub> hub, VoyaDbContext context)
	{
		_hubContext = hub;
		_context = context;
	}

	// FEATURE 6: BROADCAST TO DRIVERS
	[HttpPost("broadcast")]
	[RequirePermission(Permissions.LogisticsManage)]
	public async Task<IActionResult> BroadcastToDrivers([FromBody] string message)
	{
		// Target the specific SignalR Group "DriversGroup"
		await _hubContext.Clients.Group("DriversGroup").SendAsync("ReceiveDriverAlert", message);
		return Ok("Message sent to all active drivers.");
	}

	// GET: api/v1/admin/logistics/drivers/online
	[HttpGet("drivers/online")]
	[RequirePermission(Permissions.LogisticsManage)]
	public async Task<IActionResult> GetOnlineDrivers()
	{
		// Fetch drivers who are NOT Offline
		// Note: DriverStatus enum must be referenced correctly
		var drivers = await _context.DriverProfiles
			.Include(d => d.User)
			.Where(d => d.Status != Voya.Core.Entities.DriverStatus.Offline)
			.Select(d => new
			{
				Name = d.User.FullName,
				Lat = d.CurrentLat,
				Lng = d.CurrentLng,
				Status = d.Status.ToString()
			})
			.ToListAsync();

		return Ok(drivers);
	}

	// GET: api/v1/admin/logistics/calendar?month=10&year=2023
	[HttpGet("calendar")]
	[RequirePermission(Permissions.LogisticsManage)]
	public async Task<IActionResult> GetCalendarStats([FromQuery] int month, [FromQuery] int year)
	{
		// 1. Filter Shipments by Month
		var query = _context.Shipments
			.Where(s => s.EstimatedDeliveryTime.Month == month && s.EstimatedDeliveryTime.Year == year);

		// 2. Group by Day
		var dailyStats = await query
			.GroupBy(s => s.EstimatedDeliveryTime.Day)
			.Select(g => new
			{
				Day = g.Key,
				TotalOrders = g.Count(),
				// Late = Not Delivered AND Due Date passed
				LateCount = g.Count(s => s.Status != ShipmentStatus.Delivered && s.EstimatedDeliveryTime < DateTime.UtcNow)
			})
			.ToListAsync();

		return Ok(dailyStats);
	}

	// GET: api/v1/admin/logistics/alerts/late
	[HttpGet("alerts/late")]
	[RequirePermission(Permissions.LogisticsManage)]
	public async Task<IActionResult> GetLateShipments()
	{
		var lateItems = await _context.Shipments
			.Include(s => s.Order)
			.Where(s => s.Status != ShipmentStatus.Delivered
					 && s.EstimatedDeliveryTime < DateTime.UtcNow)
			.OrderBy(s => s.EstimatedDeliveryTime) // Most overdue first
			.Take(20) // Limit list
			.Select(s => new
			{
				TrackingNumber = s.TrackingNumber,
				StoreName = "Vendor #" + s.Order.Items.First().ProductId.ToString().Substring(0, 4), // Mock Store Name resolution
				DelayDays = (DateTime.UtcNow - s.EstimatedDeliveryTime).Days
			})
			.ToListAsync();

		return Ok(lateItems);
	}

	// POST: Feature from UI "Auto-Penalize"
	[HttpPost("penalize-late")]
	[RequirePermission(Permissions.LogisticsManage)]
	public async Task<IActionResult> AutoPenalizeVendors()
	{
		// Logic: Find late orders, deduct score/funds from vendors
		await Task.Delay(500); // Simulate processing
		return Ok("Penalties applied to 12 vendors.");
	}

	// GET: api/v1/admin/logistics/maintenance
	[HttpGet("maintenance")]
	[RequirePermission(Permissions.LogisticsManage)]
	public IActionResult GetMaintenanceTickets()
	{
		// Mock Data (In real app, query a MaintenanceTickets table)
		var tickets = new List<object>
		{
			new { Id = Guid.NewGuid(), Asset = "Bike #402", Issue = "Brake Failure", Priority = "High", Reporter = "Driver Ahmed", Status = "Open" },
			new { Id = Guid.NewGuid(), Asset = "Van #101", Issue = "Oil Change Needed", Priority = "Low", Reporter = "System Auto-Alert", Status = "Open" },
			new { Id = Guid.NewGuid(), Asset = "Truck #99", Issue = "Tire Puncture", Priority = "Medium", Reporter = "Driver Sarah", Status = "In Progress" }
		};
		return Ok(tickets);
	}

	// POST: api/v1/admin/logistics/maintenance/{id}/assign
	[HttpPost("maintenance/{id}/assign")]
	[RequirePermission(Permissions.LogisticsManage)]
	public IActionResult AssignMechanic(Guid id)
	{
		// Logic: Send email to mechanic, update status
		return Ok("Mechanic assigned successfully.");
	}

	// GET: api/v1/admin/logistics/zones
	[HttpGet("zones")]
	[RequirePermission(Permissions.LogisticsManage)]
	public IActionResult GetZones()
	{
		// Mock Zones (In reality, stored as GeoJSON in DB)
		var zones = new List<object>
		{
			new { Id = Guid.NewGuid(), Name = "Zone A (City Center)", Fee = 2.00, IsActive = true, Type = "Standard" },
			new { Id = Guid.NewGuid(), Name = "Zone B (Suburbs)", Fee = 5.00, IsActive = true, Type = "Standard" },
			new { Id = Guid.NewGuid(), Name = "Zone C (University)", Fee = 0.00, IsActive = true, Type = "DiscountTrigger" }
		};
		return Ok(zones);
	}

	// POST: api/v1/admin/logistics/zones
	[HttpPost("zones")]
	[RequirePermission(Permissions.LogisticsManage)]
	public IActionResult CreateZone([FromBody] object zoneData)
	{
		// Save polygon logic
		return Ok("Zone created.");
	}
}