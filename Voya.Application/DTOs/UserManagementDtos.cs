namespace Voya.Application.DTOs;

// Address DTOs
public record AddressDto(Guid Id, string Street, string City, string State, string ZipCode, string Country);
public record CreateAddressRequest(string Street, string City, string State, string ZipCode, string Country, string PhoneNumber);

// Payment DTOs
public record PaymentMethodDto(Guid Id, string Type, string DisplayName, bool IsDefault);
public record CreatePaymentMethodRequest(string CardNumber, string Expiry, string Cvv, string CardHolderName);

// Checkout - Updated for Multi-Address Support
public record CheckoutRequest
{
	// Single Address Mode (Legacy)
	public Guid? AddressId { get; set; }

	// Multi-Address Mode
	public bool IsMultiAddress { get; set; } = false;
	public List<ItemAddressMapping>? MultiAddressMap { get; set; }

	// Payment & Vouchers
	public string PaymentType { get; set; } = "CreditCard"; // "CreditCard" or "CashOnDelivery"
	public Guid? PaymentMethodId { get; set; }
	public string? VoucherCode { get; set; }
	public bool UsePoints { get; set; }

	// Gift Wrap
	public bool IsGift { get; set; } = false;
	public string? GiftMessage { get; set; }
	public Guid? GiftWrapOptionId { get; set; }
}

public class ItemAddressMapping
{
	public Guid ProductId { get; set; }
	public Guid AddressId { get; set; }
	public int Quantity { get; set; }
}