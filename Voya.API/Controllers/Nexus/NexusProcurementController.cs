using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voya.API.Attributes;
using Voya.Core.Constants;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/procurement")]
public class NexusProcurementController : ControllerBase
{
	// Mock Database for Internal Supplies
	private static readonly List<object> _supplies = new()
	{
		new { Id = 1, Name = "Bubble Wrap (Roll)", Price = 15.00, Icon = "layers" },
		new { Id = 2, Name = "Voya Branded Tape", Price = 5.50, Icon = "ad_units" },
		new { Id = 3, Name = "Thermal Printer Paper", Price = 2.00, Icon = "receipt" },
		new { Id = 4, Name = "Office Coffee (Bulk)", Price = 40.00, Icon = "coffee" }
	};

	[HttpGet("supplies")]
	[RequirePermission(Permissions.SystemConfig)] // Or specific procurement permission
	public IActionResult GetSupplies()
	{
		return Ok(_supplies);
	}

	[HttpPost("order")]
	[RequirePermission(Permissions.SystemConfig)]
	public IActionResult PlaceSupplyOrder([FromBody] int itemId)
	{
		// Logic: Send email to supplier, deduct from budget
		return Ok("Order placed successfully. Approval pending.");
	}
}