using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/security")]
public class NexusSecurityController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public NexusSecurityController(VoyaDbContext context)
	{
		_context = context;
	}

	private Guid GetAdminId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

	// FEATURE 4: GHOST LOGIN (Impersonation)
	[HttpPost("impersonate/{userId}")]
	public async Task<IActionResult> ImpersonateUser(Guid userId)
	{
		var targetUser = await _context.Users.FindAsync(userId);
		if (targetUser == null) return NotFound("User not found");

		// 1. Audit Log (Critical!)
		_context.SensitiveAccessLogs.Add(new SensitiveAccessLog
		{
			AdminUserId = GetAdminId(),
			Reason = "Ghost Login / Impersonation",
			ResourceAccessed = $"Logged in as {targetUser.Email}"
		});
		await _context.SaveChangesAsync();

		// 2. Return Data required to generate a token
		// In a real app, you would inject 'IJwtTokenGenerator' and return the actual token string.
		// For now, we confirm success.
		return Ok(new
		{
			Message = "Impersonation approved.",
			TargetUserEmail = targetUser.Email,
			Note = "Use the Token Generator logic to create a JWT for this user now."
		});
	}

	// FEATURE 7: SENSITIVE ACCESS LOGS
	[HttpGet("sensitive-logs")]
	public async Task<IActionResult> GetSensitiveLogs()
	{
		var logs = await _context.SensitiveAccessLogs
			.OrderByDescending(l => l.Timestamp)
			.Take(100)
			.ToListAsync();
		return Ok(logs);
	}

	// FEATURE 8: IP FIREWALL
	[HttpPost("firewall/block")]
	public async Task<IActionResult> BlockIp([FromBody] IpBlockRule rule)
	{
		rule.BlockedByAdminId = GetAdminId();
		_context.IpBlockRules.Add(rule);
		await _context.SaveChangesAsync();
		return Ok($"IP {rule.IpAddress} blocked.");
	}

	// FEATURE 9: API KEYS
	[HttpPost("api-keys")]
	public async Task<IActionResult> CreateApiKey(string partnerName)
	{
		var rawKey = Guid.NewGuid().ToString("N"); // In real app, make longer/secure
		var keyEntity = new ExternalApiKey
		{
			PartnerName = partnerName,
			ApiKeyHash = rawKey, // In real app, Hash this!
			ExpiresAt = DateTime.UtcNow.AddYears(1)
		};

		_context.ExternalApiKeys.Add(keyEntity);
		await _context.SaveChangesAsync();

		return Ok(new { Note = "Save this key, it won't be shown again", Key = rawKey });
	}
}