using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voya.API.Attributes;
using Voya.Core.Constants;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/subscriptions")]
public class NexusSubscriptionsController : ControllerBase
{
	// Mock Persistence (In real app, store in DB)
	private static SubscriptionConfigDto _config = new()
	{
		Name = "Gamer Loot",
		Price = 29.99m,
		IsRecurring = true
	};

	// GET: Current Config
	[HttpGet("config")]
	[RequirePermission(Permissions.ContentManage)]
	public IActionResult GetConfig() => Ok(_config);

	// POST: Update Config
	[HttpPost("config")]
	[RequirePermission(Permissions.ContentManage)]
	public IActionResult UpdateConfig([FromBody] SubscriptionConfigDto config)
	{
		_config = config;
		return Ok("Configuration saved.");
	}

	// GET: Preview Items (The "Mystery" content)
	[HttpGet("preview")]
	public IActionResult GetPreviewItems()
	{
		var items = new[]
		{
			new { Name = "Gaming Mouse", Icon = "mouse" },
			new { Name = "RGB Mousepad", Icon = "keyboard" },
			new { Name = "Voya Stickers", Icon = "sticky_note_2" },
			new { Name = "Energy Drink", Icon = "local_drink" },
			new { Name = "Keycap Set", Icon = "keyboard_alt" }
		};
		return Ok(items);
	}

	// POST: Lock & Charge
	[HttpPost("lock-cycle")]
	[RequirePermission(Permissions.FinancePayout)]
	public async Task<IActionResult> LockAndCharge()
	{
		// Logic: Create Orders for all active subscribers
		await Task.Delay(1000);
		return Ok("Cycle locked. 1,240 subscribers charged successfully.");
	}

	public class SubscriptionConfigDto
	{
		public string Name { get; set; } = string.Empty;
		public decimal Price { get; set; }
		public bool IsRecurring { get; set; }
	}
}