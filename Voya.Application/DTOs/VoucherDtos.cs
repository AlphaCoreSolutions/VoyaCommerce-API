using Voya.Core.Entities;

namespace Voya.Application.DTOs;

public record VoucherDto(
	Guid Id,
	string Code,
	string Description,
	decimal DiscountValue,
	string Type,
	DateTime ExpiryDate
);

public record ClaimVoucherRequest(string Code);

// Used for the "Wishlist Nudge" feature
public class SendTargetedOfferDto
{
	public Guid ProductId { get; set; }
	public decimal DiscountPercentage { get; set; }
}

// Used for creating a new Voucher (POST)
public class CreateVoucherDto
{
	public string Code { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;

	// We use the Enum directly here for type safety
	public DiscountType Type { get; set; }

	public decimal Value { get; set; }
	public DateTime StartDate { get; set; }
	public DateTime EndDate { get; set; }
	public int MaxUsesPerUser { get; set; }
}