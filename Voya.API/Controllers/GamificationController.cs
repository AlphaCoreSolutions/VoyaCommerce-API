using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

	public record PlayGameRequest(string GameId);

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
			// Calculate time until reset
			var tomorrow = today.AddDays(1);
			var timeRemaining = tomorrow - DateTime.UtcNow;
			return StatusCode(403, $"Daily limit reached! Next play in {timeRemaining.Hours}h {timeRemaining.Minutes}m.");
		}

		// 3. Determine Reward Type (1 in 8 Chance for Voucher)
		var random = new Random();
		bool wonVoucher = random.Next(1, 9) == 1; // Returns 1 to 8. If 1, Win Voucher.

		string rewardType = "Points";
		string rewardValue = "";
		string message = "";

		if (wonVoucher)
		{
			// === VOUCHER REWARD LOGIC ===
			rewardType = "Voucher";

			// Generate a unique code (e.g., GAME-X7Z9)
			string code = $"WIN-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";

			// Randomize Discount: 10% to 30%
			int discountPercent = random.Next(10, 31);

			// Create the Voucher Entity
			var voucher = new Voucher
			{
				Code = code,
				Description = $"Won in {request.GameId}",
				Type = DiscountType.Percentage,
				Value = discountPercent,
				StartDate = DateTime.UtcNow,
				EndDate = DateTime.UtcNow.AddDays(7), // Valid for 7 days
				IsActive = true,
				MaxUses = 1,
				MaxUsesPerUser = 1
			};

			// Link to User
			var userVoucher = new UserVoucher
			{
				UserId = userId,
				Voucher = voucher, // EF Core handles ID assignment
				UsageCount = 0
			};

			_context.Vouchers.Add(voucher);
			_context.UserVouchers.Add(userVoucher);

			rewardValue = code;
			message = $"Jackpot! You won a {discountPercent}% OFF Voucher: {code}";
		}
		else
		{
			// === POINTS REWARD LOGIC ===
			// Give 5 to 25 Points
			int pointsWon = random.Next(5, 26); // 26 is exclusive

			user.PointsBalance += pointsWon;

			rewardType = "Points";
			rewardValue = pointsWon.ToString();
			message = $"You won {pointsWon} Points!";
		}

		// 4. Update User Stats
		user.DailyGameCount++;
		user.LastGameDate = DateTime.UtcNow;

		// 5. Save History
		var history = new GameHistory
		{
			UserId = userId,
			GameName = request.GameId,
			PointsEarned = rewardType == "Points" ? int.Parse(rewardValue) : 0,
			PlayedAt = DateTime.UtcNow
		};
		_context.GameHistories.Add(history);

		await _context.SaveChangesAsync();

		// 6. Return Result
		return Ok(new
		{
			Success = true,
			Game = request.GameId,
			RewardType = rewardType, // "Points" or "Voucher"
			RewardValue = rewardValue, // "15" or "GAME-X9Z"
			Message = message,
			GamesRemaining = 3 - user.DailyGameCount
		});
	}
}