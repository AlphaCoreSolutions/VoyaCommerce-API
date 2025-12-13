using System.ComponentModel.DataAnnotations.Schema;

namespace Voya.Core.Entities;

public class AffiliateProfile
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public Guid UserId { get; set; }
	public User User { get; set; } = null!;

	public string PromoCode { get; set; } = string.Empty; // e.g. "SARAH20"
	public decimal CommissionRate { get; set; } = 0.10m; // 10%
	public decimal TotalSales { get; set; } = 0;
	public decimal PendingPayout { get; set; } = 0;
	public bool IsApproved { get; set; } = false;
}