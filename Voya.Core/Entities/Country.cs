using Voya.Core.Entities;

public class Country
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Name { get; set; } = string.Empty;
	public string IsoCode { get; set; } = string.Empty; // "JO"
	public string PhoneCode { get; set; } = string.Empty; // "+962"

	public Guid CurrencyId { get; set; }
	public Currency Currency { get; set; } = null!;

	public bool IsActive { get; set; } = true;
}