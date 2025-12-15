using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/user")]
public class UserController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public UserController(VoyaDbContext context)
	{
		_context = context;
	}

	[HttpGet("profile")]
	public async Task<IActionResult> GetProfile()
	{
		// Parse ID safely
		var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

		// Fetch User
		var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
		if (user == null) return NotFound();

		// Return exact structure expected by Flutter
		return Ok(new
		{
			user.Id,
			user.FullName,
			user.Email,
			user.AvatarUrl,          // Added
			user.PointsBalance,
			user.CurrentStreak,
			user.IsGoldMember,
			user.ReferralCode,       // Added
			user.MemberDiscountPercent // Added
		});
	}
}