namespace Voya.Application.DTOs;

public record CheckInResponse(
	bool Success,
	int PointsEarned,
	int CurrentStreak,
	string Message,
	bool IsBigPrize // e.g. 7-day streak
);

public record MysteryBoxResponse(
	bool Success,
	string RewardType, // "Points" or "Coupon"
	string RewardValue, // "50" or "SAVE20"
	string Message
);