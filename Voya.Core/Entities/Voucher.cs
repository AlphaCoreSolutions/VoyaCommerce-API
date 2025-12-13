using System.ComponentModel.DataAnnotations.Schema;

namespace Voya.Core.Entities;

public enum DiscountType { FixedAmount, Percentage }

public class Voucher
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public string Code { get; set; } = string.Empty; // e.g., "WELCOME20"
	public string Description { get; set; } = string.Empty;

	public DiscountType Type { get; set; } = DiscountType.FixedAmount;
	public decimal Value { get; set; } // e.g., 20.00 (dollars) or 10 (percent)

	// Validity Rules
	public DateTime StartDate { get; set; } = DateTime.UtcNow;
	public DateTime EndDate { get; set; }
	public bool IsActive { get; set; } = true;

	// FIX 1: Added CreatedAt for sorting/audit
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	// FIX 2: Added MaxUses (Total global usage limit)
	public int MaxUses { get; set; } = 1000;

	// How many times can ONE user use this? (Default 1)
	public int MaxUsesPerUser { get; set; } = 1;

	public Guid? StoreId { get; set; } // Null = Global Voucher (Admin), Not Null = Store Voucher
	public Store? Store { get; set; }  // Navigation property
}

public class UserVoucher
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid UserId { get; set; }
	public Guid VoucherId { get; set; }

	public int UsageCount { get; set; } = 0; // Tracks how many times used
	public DateTime? DateClaimed { get; set; } = DateTime.UtcNow;

	public Voucher Voucher { get; set; } = null!;
}