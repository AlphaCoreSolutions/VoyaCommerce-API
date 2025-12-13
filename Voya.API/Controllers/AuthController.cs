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
		if (await _context.Users.AnyAsync(u => u.Email == request.Email))
			return BadRequest("User exists.");

		// Generate a unique referral code for this new user
		string myReferralCode = request.FullName.Replace(" ", "").ToUpper()[..3] + new Random().Next(100, 999);

		var user = new User
		{
			Email = request.Email,
			FullName = request.FullName,
			PasswordHash = _passwordHasher.HashPassword(request.Password),
			PointsBalance = 10,
			ReferralCode = myReferralCode
		};

		// === REFERRAL LOGIC ===
		if (!string.IsNullOrEmpty(request.ReferralCode))
		{
			var referrer = await _context.Users.FirstOrDefaultAsync(u => u.ReferralCode == request.ReferralCode);
			if (referrer != null)
			{
				// Link them
				user.ReferredByUserId = referrer.Id;

				// Bonus for New User
				user.PointsBalance += 50;

				// Bonus for Referrer
				referrer.PointsBalance += 50;

				// (Optional) Notify Referrer
				// _notificationService.Notify(referrer.Id, "You invited a friend! +50 Points");
			}
		}

		_context.Users.Add(user);
		await _context.SaveChangesAsync();

		var token = _jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email, user.IsGoldMember);
		return Ok(new AuthResponse(user.Id, user.Email, token));
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

		// RETURN: Core Data + Gamification Stats
		// EXCLUDE: Orders, Addresses, PasswordHash
		return Ok(new
		{
			id = user.Id,
			email = user.Email,
			fullName = user.FullName,
			avatarUrl = user.AvatarUrl, // Nullable

			// Gamification & Loyalty
			pointsBalance = user.PointsBalance,
			isGoldMember = user.IsGoldMember,
			currentStreak = user.CurrentStreak,

			// Referral
			referralCode = user.ReferralCode,

			// Computed Logic (sent as value)
			memberDiscountPercent = user.MemberDiscountPercent,

			// JWT
			token = token
		});
	}

	// DTOs
	public record RegisterRequest(string Email, string Password, string FullName, string? ReferralCode);
	public record LoginRequest(string Email, string Password);
	public record AuthResponse(Guid Id, string Email, string Token);
}