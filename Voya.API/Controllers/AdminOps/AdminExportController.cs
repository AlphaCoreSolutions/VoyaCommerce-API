using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Voya.API.Attributes;
using Voya.Core.Constants;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.AdminOps;

[Authorize]
[ApiController]
[Route("api/v1/admin/export")]
public class AdminExportController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public AdminExportController(VoyaDbContext context) { _context = context; }

	// FEATURE 10: EXPORT ORDERS (CSV Streaming)
	[HttpGet("orders")]
	[RequirePermission(Permissions.FinanceView)]
	public async Task<IActionResult> ExportOrdersCsv()
	{
		var orders = await _context.Orders
			.Include(o => o.User)
			.OrderByDescending(o => o.PlacedAt)
			.Take(1000) // Limit for safety
			.ToListAsync();

		var builder = new StringBuilder();
		builder.AppendLine("OrderId,Date,Customer,Total,Status");

		foreach (var o in orders)
		{
			builder.AppendLine($"{o.Id},{o.PlacedAt},{o.User?.FullName ?? "Guest"},{o.TotalAmount},{o.Status}");
		}

		return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", $"orders_{DateTime.Now:yyyyMMdd}.csv");
	}
}