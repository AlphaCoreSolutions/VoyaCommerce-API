using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.AdminOps;

[Authorize]
[ApiController]
[Route("api/v1/admin/search")]
public class AdminSearchController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public AdminSearchController(VoyaDbContext context) { _context = context; }

	// FEATURE 10: GLOBAL SEARCH (Omnibar)
	[HttpGet]
	public async Task<IActionResult> GlobalSearch([FromQuery] string q)
	{
		if (string.IsNullOrWhiteSpace(q)) return BadRequest();

		// Parallel execution
		var usersTask = _context.Users
			.Where(u => u.Email.Contains(q) || u.FullName.Contains(q))
			.Select(u => new { Type = "User", u.Id, Title = u.Email, Subtitle = u.FullName })
			.Take(5).ToListAsync();

		var ordersTask = _context.Orders
			.Where(o => o.Id.ToString().Contains(q) || o.TrackingNumber!.Contains(q))
			.Select(o => new { Type = "Order", o.Id, Title = "Order " + o.Id, Subtitle = o.TotalAmount.ToString() })
			.Take(5).ToListAsync();

		var ticketsTask = _context.Tickets
			.Where(t => t.Subject.Contains(q) || t.Id.ToString().Contains(q))
			.Select(t => new { Type = "Ticket", t.Id, Title = t.Subject, Subtitle = t.Status.ToString() })
			.Take(5).ToListAsync();

		await Task.WhenAll(usersTask, ordersTask, ticketsTask);

		var results = usersTask.Result.Concat(ordersTask.Result).Concat(ticketsTask.Result);
		return Ok(results);
	}
}