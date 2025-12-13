using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.Application.DTOs;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/gamification")]
public class GamificationController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public GamificationController(VoyaDbContext context)
	{
		_context = context;
	}

	private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

	// DTO for the request
	public record PlayGameRequest(string GameId); // e.g., "spin_wheel"

	[HttpPost("play")]
	public async Task<IActionResult> PlayGame(PlayGameRequest request)
	{
		var userId = GetUserId();
		var user = await _context.Users.FindAsync(userId);
		if (user == null) return Unauthorized();

		var today = DateTime.UtcNow.Date;

		// 1. Reset Counter if it's a new day
		if (user.LastGameDate?.Date != today)
		{
			user.DailyGameCount = 0;
			user.LastGameDate = DateTime.UtcNow;
		}

		// 2. Check Limit (Max 3 games per day)
		if (user.DailyGameCount >= 3)
		{
			// Calculate time until reset (tomorrow midnight)
			var tomorrow = today.AddDays(1);
			var timeRemaining = tomorrow - DateTime.UtcNow;
			return BadRequest($"Daily limit reached! You can play again in {timeRemaining.Hours}h {timeRemaining.Minutes}m.");
		}

		// 3. Play the Game (Weighted Random Logic)
		// Logic: 
		// - 5 to 20 points: Very Common (~90%)
		// - 21 to 45 points: Rare (~9%)
		// - 46 to 50 points: Legendary (~1%)

		var random = new Random();
		int roll = random.Next(1, 101); // 1-100
		int pointsWon = 0;

		if (roll <= 90)
		{
			// Common: 5 to 20
			pointsWon = random.Next(5, 21);
		}
		else if (roll <= 99)
		{
			// Rare: 21 to 45
			pointsWon = random.Next(21, 46);
		}
		else
		{
			// Legendary: 46 to 50
			pointsWon = random.Next(46, 51);
		}

		// 4. Update User
		user.PointsBalance += pointsWon;
		user.DailyGameCount++;
		user.LastGameDate = DateTime.UtcNow;

		// 5. Save History (Analytics)
		var history = new GameHistory
		{
			UserId = userId,
			GameName = request.GameId,
			PointsEarned = pointsWon,
			PlayedAt = DateTime.UtcNow
		};
		_context.GameHistories.Add(history);

		await _context.SaveChangesAsync();

		return Ok(new
		{
			Success = true,
			Game = request.GameId,
			PointsWon = pointsWon,
			TotalPoints = user.PointsBalance,
			GamesPlayedToday = user.DailyGameCount,
			GamesRemaining = 3 - user.DailyGameCount
		});
	}

	// NEW Endpoint: Admin Analytics (Simple version)
	[HttpGet("history")]
	public async Task<ActionResult> GetMyGameHistory()
	{
		var userId = GetUserId();
		var history = await _context.GameHistories
			.Where(g => g.UserId == userId)
			.OrderByDescending(g => g.PlayedAt)
			.Take(20) // Last 20 games
			.Select(g => new
			{
				g.GameName,
				g.PointsEarned,
				Date = g.PlayedAt.ToString("yyyy-MM-dd HH:mm")
			})
			.ToListAsync();

		return Ok(history);
	}

	[HttpPost("check-in")]
	public async Task<ActionResult<CheckInResponse>> DailyCheckIn()
	{
		var userId = GetUserId();
		var user = await _context.Users.FindAsync(userId);

		if (user == null) return Unauthorized();

		var today = DateTime.UtcNow.Date;
		var lastCheckIn = user.LastCheckInDate?.Date;

		// 1. Check if already checked in today
		if (lastCheckIn == today)
		{
			return BadRequest("You have already checked in today!");
		}

		// 2. Calculate Streak
		bool isConsecutive = lastCheckIn.HasValue && lastCheckIn == today.AddDays(-1);
		if (isConsecutive)
		{
			user.CurrentStreak++;
		}
		else
		{
			user.CurrentStreak = 1; // Reset streak
		}

		user.LastCheckInDate = DateTime.UtcNow;

		// 3. Determine Reward
		int points = 10; // Standard reward
		bool bigPrize = false;
		string msg = "Daily check-in successful! +10 Points.";

		// Big Prize every 7 days
		if (user.CurrentStreak % 7 == 0)
		{
			points = 100;
			bigPrize = true;
			msg = "WOW! 7-Day Streak! You earned 100 Points!";
		}

		user.PointsBalance += points;
		await _context.SaveChangesAsync();

		return Ok(new CheckInResponse(true, points, user.CurrentStreak, msg, bigPrize));
	}

	[HttpPost("mystery-box")]
	public async Task<ActionResult<MysteryBoxResponse>> OpenMysteryBox()
	{
		var userId = GetUserId();
		var user = await _context.Users.FindAsync(userId);

		// Cost to open box? Let's say it's free once a day, or costs 50 points.
		// For this MVP, let's make it cost 50 points.
		if (user!.PointsBalance < 50)
		{
			return BadRequest("Not enough points! You need 50 points to open a Mystery Box.");
		}

		user.PointsBalance -= 50;

		// Random Prize Logic
		var random = new Random();
		var roll = random.Next(1, 101); // 1 to 100

		string type;
		string value;
		string message;

		if (roll > 90) // 10% Chance of Coupon
		{
			type = "Coupon";
			value = "LUCKY" + random.Next(1000, 9999);
			message = $"Jackpot! You won a coupon code: {value}";
			// In a real app, save this coupon to a 'UserCoupons' table here
		}
		else // 90% Chance of returning points (sometimes more, sometimes less)
		{
			type = "Points";
			int pointsWon = random.Next(10, 100); // Win back 10 to 100 points
			value = pointsWon.ToString();
			message = $"You won {pointsWon} points!";
			user.PointsBalance += pointsWon;
		}

		await _context.SaveChangesAsync();

		return Ok(new MysteryBoxResponse(true, type, value, message));
	}
}