using System.Net;

namespace Voya.Core.Entities;

public class User
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Email { get; set; } = string.Empty;
	public string PasswordHash { get; set; } = string.Empty;
	public string FullName { get; set; } = string.Empty;
	public string PhoneNumber { get; set; }
	public string? AvatarUrl { get; set; }

	// Gamification & Loyalty
	public int PointsBalance { get; set; } = 0;
	public bool IsGoldMember { get; set; } = false;

	// Streak Logic
	public DateTime? LastCheckInDate { get; set; }
	public int CurrentStreak { get; set; } = 0;

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	// Navigation Properties
	public ICollection<Order> Orders { get; set; } = new List<Order>();
	public ICollection<Address> Addresses { get; set; } = new List<Address>();

	public string ReferralCode { get; set; } = string.Empty; // e.g. "ALEX882"
	public Guid? ReferredByUserId { get; set; } // Who invited me?

	public decimal TotalSpentLifetime { get; set; } = 0; // Track spending

	// Computed property (no DB storage needed, just logic)
	public decimal MemberDiscountPercent => TotalSpentLifetime switch
	{
		> 1000 => 0.10m, // Platinum: 10% off
		> 500 => 0.05m,  // Gold: 5% off
		_ => 0.0m
	};

	// Gamification Limits
	public int DailyGameCount { get; set; } = 0; // Tracks games played today
	public DateTime? LastGameDate { get; set; }  // To reset counter tomorrow

	// Nexus Access
	public Guid? NexusRoleId { get; set; } // Null = Regular User/Seller, Not Null = Employee
	public NexusRole? NexusRole { get; set; }

	public bool IsBanned { get; set; } = false;
	public string? BanReason { get; set; }
	public decimal WalletBalance { get; set; } = 0; // Internal Wallet
	public double TrustScore { get; set; } = 100.0; // Default 100
}