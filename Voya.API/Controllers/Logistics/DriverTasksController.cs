using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Voya.Core.Entities; // Required for Shipment, ShipmentStatus
using Voya.Core.Enums;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.DriverApp;

[Authorize]
[ApiController]
[Route("api/v1/driver-app")]
public class DriverTasksController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public DriverTasksController(VoyaDbContext context)
	{
		_context = context;
	}

	private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

	// GET: My Daily Run
	[HttpGet("tasks")]
	public async Task<IActionResult> GetMyTasks()
	{
		var userId = GetUserId();

		// Find Driver Profile
		var driver = await _context.DriverProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
		if (driver == null) return BadRequest("User is not a registered driver.");

		// Get Shipments assigned to this driver
		// Note: We use explicit LINQ to avoid "Queryable.Order" confusion
		var tasks = await _context.Shipments
			.Include(s => s.Order)
			.ThenInclude(o => o.User)
			.Where(s => s.DriverId == driver.Id &&
					   (s.Status == ShipmentStatus.Assigned || s.Status == ShipmentStatus.OutForDelivery))
			.OrderBy(s => s.EstimatedDeliveryTime) // Standard LINQ OrderBy
			.Select(s => new
			{
				TaskId = s.Id,
				OrderId = s.OrderId,
				Type = "Dropoff",
				Time = s.EstimatedDeliveryTime.ToString("hh:mm tt"),
				// Accessing the nested User property safely
				Title = "Dropoff: " + (s.Order.User != null ? s.Order.User.FullName : "Guest"),
				Subtitle = s.CurrentLocation + " • Ref #" + s.TrackingNumber,
				IsCompleted = s.Status == ShipmentStatus.Delivered,
				IsActive = s.Status == ShipmentStatus.OutForDelivery
			})
			.ToListAsync();

		return Ok(tasks);
	}

	// POST: Complete Delivery
	[HttpPost("tasks/{taskId}/complete")]
	public async Task<IActionResult> CompleteTask(Guid taskId)
	{
		var shipment = await _context.Shipments.FindAsync(taskId);
		if (shipment == null) return NotFound();

		shipment.Status = ShipmentStatus.Delivered;
		shipment.ActualDeliveryTime = DateTime.UtcNow;

		// Also update parent Order status
		var order = await _context.Orders.FindAsync(shipment.OrderId);
		if (order != null)
		{
			order.Status = OrderStatus.Delivered;
			order.PaymentStatus = PaymentStatus.Paid;
		}

		await _context.SaveChangesAsync();
		return Ok("Delivery confirmed.");
	}
}