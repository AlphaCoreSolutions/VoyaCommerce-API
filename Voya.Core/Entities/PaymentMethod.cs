namespace Voya.Core.Entities;

public class PaymentMethod
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid UserId { get; set; }

	// e.g., "Visa", "MasterCard", "PayPal"
	public string Type { get; set; } = string.Empty;

	// e.g., "Visa ending in 4242"
	public string DisplayName { get; set; } = string.Empty;

	// In a real app, this is a token from Stripe/PayPal. Never store raw card numbers!
	// For this mock, we will store a dummy "Token".
	public string ProviderToken { get; set; } = string.Empty;

	public bool IsDefault { get; set; } = false;
}