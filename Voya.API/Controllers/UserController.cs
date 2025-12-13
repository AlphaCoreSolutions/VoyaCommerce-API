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
		var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

		var user = await _context.Users
			.Select(u => new
			{
				u.Id,
				u.FullName,
				u.Email,
				u.PointsBalance,
				u.CurrentStreak,
				u.IsGoldMember
			})
			.FirstOrDefaultAsync(u => u.Id == userId);

		return Ok(user);
	}
}