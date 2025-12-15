using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.Application.Common.Interfaces;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
	private readonly VoyaDbContext _context;
	private readonly IPasswordHasher _passwordHasher;
	private readonly IJwtTokenGenerator _jwtTokenGenerator;

	public AuthController(VoyaDbContext context, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator)
	{
		_context = context;
		_passwordHasher = passwordHasher;
		_jwtTokenGenerator = jwtTokenGenerator;
	}

	[AllowAnonymous]
	[HttpPost("register")]
	public async Task<IActionResult> Register(RegisterRequest request)
	{
		try
		{
			if (await _context.Users.AnyAsync(u => u.Email == request.Email))
				return BadRequest("User already exists.");

			// 1. SAFE REFERRAL GENERATION (Fixes crash on short names)
			var cleanName = request.FullName.Replace(" ", "").ToUpper();
			// If name is "Al", pad it to "ALX"; if "Moe", keep "MOE"
			var prefix = cleanName.Length >= 3 ? cleanName[..3] : cleanName.PadRight(3, 'X');
			string myReferralCode = prefix + new Random().Next(100, 999);

			var user = new User
			{
				Email = request.Email,
				FullName = request.FullName,
				PasswordHash = _passwordHasher.HashPassword(request.Password),
				PointsBalance = 10,
				ReferralCode = myReferralCode,

				// 2. SET DEFAULTS (Fixes DB Constraint Errors)
				CreatedAt = DateTime.UtcNow,
				IsActive = true,
				IsGoldMember = false,
				CurrentStreak = 0,
				AvatarUrl = "",
				PhoneNumber = ""
			};

			// 3. REFERRAL LOGIC
			if (!string.IsNullOrEmpty(request.ReferralCode))
			{
				var referrer = await _context.Users.FirstOrDefaultAsync(u => u.ReferralCode == request.ReferralCode);
				if (referrer != null)
				{
					user.ReferredByUserId = referrer.Id;
					user.PointsBalance += 50;
					referrer.PointsBalance += 50;
				}
			}

			_context.Users.Add(user);
			await _context.SaveChangesAsync();

			var token = _jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email, user.IsGoldMember);
			return Ok(new AuthResponse(user.Id, user.Email, token));
		}
		catch (Exception ex)
		{
			// 4. EXPOSE THE REAL ERROR
			// This allows us to see "Relation 'Wallets' does not exist" or similar in the API response
			return StatusCode(500, new
			{
				message = "Registration Failed",
				error = ex.Message,
				inner = ex.InnerException?.Message
			});
		}
	}

	[AllowAnonymous]
	[HttpPost("login")]
	public async Task<IActionResult> Login([FromBody] LoginRequest request)
	{
		var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
		if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
		{
			return Unauthorized("Invalid credentials.");
		}

		var token = _jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email, user.IsGoldMember);

		return Ok(new
		{
			id = user.Id,
			email = user.Email,
			fullName = user.FullName,
			avatarUrl = user.AvatarUrl,
			pointsBalance = user.PointsBalance,
			isGoldMember = user.IsGoldMember,
			currentStreak = user.CurrentStreak,
			referralCode = user.ReferralCode,
			memberDiscountPercent = user.MemberDiscountPercent,
			token = token
		});
	}

	// DTOs
	public record RegisterRequest(string Email, string Password, string FullName, string? ReferralCode);
	public record LoginRequest(string Email, string Password);
	public record AuthResponse(Guid Id, string Email, string Token);
}