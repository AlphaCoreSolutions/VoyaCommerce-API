namespace Voya.Core.Entities;

public class GlobalCoupon
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Code { get; set; } = string.Empty;
	public decimal Value { get; set; } // e.g., 10%
	public bool IsPercentage { get; set; } = true;
	public decimal MaxDiscountAmount { get; set; } // Cap at $50
	public DateTime ExpiryDate { get; set; }
	public bool IsActive { get; set; } = true;
	// Admin ID who created it
	public Guid CreatedByAdminId { get; set; }
}