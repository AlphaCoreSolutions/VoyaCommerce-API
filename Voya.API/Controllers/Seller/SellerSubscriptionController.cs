using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

[Authorize]
[ApiController]
[Route("api/v1/seller/subscriptions")]
public class SellerSubscriptionController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public SellerSubscriptionController(VoyaDbContext context) { _context = context; }

	[HttpPost]
	public async Task<IActionResult> CreatePlan([FromBody] SubscriptionPlan plan)
	{
		_context.SubscriptionPlans.Add(plan);
		await _context.SaveChangesAsync();
		return Ok("Subscription plan created.");
	}
}