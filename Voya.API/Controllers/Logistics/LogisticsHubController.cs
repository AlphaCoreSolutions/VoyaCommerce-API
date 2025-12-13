using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.Application.Interfaces.Logistics; // <--- FIX 1: Interface
using Voya.Core.Entities;
using Voya.Core.Enums;
using Voya.Infrastructure.Persistence;
using Voya.Infrastructure.Services.Logistics; // <--- FIX 2: Finds Adapters

namespace Voya.API.Controllers.Logistics;

[Authorize]
[ApiController]
[Route("api/v1/logistics/hub")]
public class LogisticsHubController : ControllerBase
{
	private readonly VoyaDbContext _context;
	private readonly InternalLogisticsAdapter _internalAdapter;
	private readonly ExternalLogisticsAdapter _externalAdapter;

	public LogisticsHubController(
		VoyaDbContext context,
		InternalLogisticsAdapter internalAdapter,
		ExternalLogisticsAdapter externalAdapter)
	{
		_context = context;
		_internalAdapter = internalAdapter;
		_externalAdapter = externalAdapter;
	}

	[HttpPost("dispatch/{orderId}")]
	public async Task<IActionResult> DispatchOrder(Guid orderId)
	{
		var order = await _context.Orders.Include(o => o.User).FirstOrDefaultAsync(o => o.Id == orderId);
		if (order == null) return NotFound();

		var provider = await _context.LogisticsProviders.FirstOrDefaultAsync(p => p.IsActive);
		if (provider == null) return BadRequest("No active logistics provider.");

		// FIX: Interface usage
		IShippingGateway gateway = provider.Type == ProviderType.Internal
			? _internalAdapter
			: _externalAdapter;

		var result = await gateway.CreateShipmentAsync(order, provider);

		// ... rest of logic ...
		return Ok(new { Tracking = result.TrackingNumber });
	}
}