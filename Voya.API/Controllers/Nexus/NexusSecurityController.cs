using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using Voya.API.Attributes;
using Voya.Core.Constants;
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

	private Guid GetAdminId()
	{
		var idStr =
			User.FindFirst(ClaimTypes.NameIdentifier)?.Value
			?? User.FindFirst("sub")?.Value
			?? User.FindFirst("id")?.Value;

		if (string.IsNullOrWhiteSpace(idStr) || !Guid.TryParse(idStr, out var id))
			throw new UnauthorizedAccessException("Invalid admin identity.");

		return id;
	}

	// -----------------------------
	// FEATURE 4: GHOST LOGIN (Impersonation)
	// POST /api/v1/nexus/security/impersonate/{userId}
	// -----------------------------
	[HttpPost("impersonate/{userId:guid}")]
	[RequirePermission(Permissions.SecurityImpersonate)]
	public async Task<IActionResult> ImpersonateUser(Guid userId)
	{
		var targetUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
		if (targetUser == null) return NotFound("User not found");

		// Audit Log (Critical)
		_context.SensitiveAccessLogs.Add(new SensitiveAccessLog
		{
			AdminUserId = GetAdminId(),
			Reason = "Ghost Login / Impersonation",
			ResourceAccessed = $"Requested impersonation for {targetUser.Email}"
		});

		await _context.SaveChangesAsync();

		// NOTE: If/when you add a token generator, generate a short-lived JWT here.
		return Ok(new
		{
			Message = "Impersonation approved.",
			TargetUserEmail = targetUser.Email,
			Note = "Implement JWT generation (short-lived) for actual impersonation."
		});
	}

	// -----------------------------
	// FEATURE 7: SENSITIVE ACCESS LOGS
	// GET /api/v1/nexus/security/sensitive-logs
	// -----------------------------
	[HttpGet("sensitive-logs")]
	[RequirePermission(Permissions.SecurityLogsView)]
	public async Task<IActionResult> GetSensitiveLogs([FromQuery] int take = 100)
	{
		if (take < 1) take = 100;
		if (take > 500) take = 500;

		var logs = await _context.SensitiveAccessLogs
			.AsNoTracking()
			.OrderByDescending(l => l.Timestamp)
			.Take(take)
			.ToListAsync();

		return Ok(logs);
	}

	// -----------------------------
	// FEATURE 8: IP FIREWALL
	// POST /api/v1/nexus/security/firewall/block
	// -----------------------------
	[HttpPost("firewall/block")]
	[RequirePermission(Permissions.SecurityFirewallManage)]
	public async Task<IActionResult> BlockIp([FromBody] IpBlockRule rule)
	{
		if (rule == null) return BadRequest("Rule is required.");
		if (string.IsNullOrWhiteSpace(rule.IpAddress)) return BadRequest("IpAddress is required.");

		rule.IpAddress = rule.IpAddress.Trim();
		rule.Reason = (rule.Reason ?? "").Trim();
		rule.BlockedByAdminId = GetAdminId();
		rule.BlockedAt = DateTime.UtcNow;

		// Optional: prevent duplicates (same IP/CIDR)
		var existing = await _context.IpBlockRules
			.FirstOrDefaultAsync(x => x.IpAddress == rule.IpAddress);

		if (existing != null)
		{
			existing.Reason = rule.Reason;
			existing.BlockedAt = DateTime.UtcNow;
			existing.BlockedByAdminId = rule.BlockedByAdminId;

			await _context.SaveChangesAsync();
			return Ok(new { Message = "IP rule updated.", IpAddress = existing.IpAddress });
		}

		_context.IpBlockRules.Add(rule);
		await _context.SaveChangesAsync();

		return Ok(new { Message = "IP blocked.", IpAddress = rule.IpAddress });
	}

	// -----------------------------
	// FEATURE 9: API KEYS
	// POST /api/v1/nexus/security/api-keys
	// -----------------------------
	public class CreateApiKeyRequest
	{
		public string PartnerName { get; set; } = "";
		public int ExpiresInDays { get; set; } = 365; // default 1 year
		public string PermissionsJson { get; set; } = "[]"; // keep your model field
	}

	[HttpPost("api-keys")]
	[RequirePermission(Permissions.SecurityApiKeysManage)]
	public async Task<IActionResult> CreateApiKey([FromBody] CreateApiKeyRequest req)
	{
		var partnerName = (req.PartnerName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(partnerName))
			return BadRequest("PartnerName is required.");

		if (req.ExpiresInDays < 1) req.ExpiresInDays = 365;
		if (req.ExpiresInDays > 3650) req.ExpiresInDays = 3650; // cap 10 years

		// Generate strong raw key (32 bytes => 64 hex chars)
		var rawKey = GenerateSecureKeyHex(32);
		var rawKeyHash = Sha256Hex(rawKey);

		var keyEntity = new ExternalApiKey
		{
			PartnerName = partnerName,
			ApiKeyHash = rawKeyHash, // store hash only
			PermissionsJson = string.IsNullOrWhiteSpace(req.PermissionsJson) ? "[]" : req.PermissionsJson,
			ExpiresAt = DateTime.UtcNow.AddDays(req.ExpiresInDays),
			IsActive = true
		};

		_context.ExternalApiKeys.Add(keyEntity);

		// Audit
		_context.SensitiveAccessLogs.Add(new SensitiveAccessLog
		{
			AdminUserId = GetAdminId(),
			Reason = "API Key Created",
			ResourceAccessed = $"Created API key for partner: {partnerName}"
		});

		await _context.SaveChangesAsync();

		return Ok(new
		{
			Note = "Save this key now. It will not be shown again.",
			Key = rawKey,
			ExpiresAtUtc = keyEntity.ExpiresAt,
			PartnerName = keyEntity.PartnerName
		});
	}

	private static string GenerateSecureKeyHex(int bytes)
	{
		var buffer = new byte[bytes];
		RandomNumberGenerator.Fill(buffer);
		return Convert.ToHexString(buffer).ToLowerInvariant();
	}

	private static string Sha256Hex(string input)
	{
		using var sha = SHA256.Create();
		var bytes = System.Text.Encoding.UTF8.GetBytes(input);
		var hash = sha.ComputeHash(bytes);
		return Convert.ToHexString(hash).ToLowerInvariant();
	}
}
