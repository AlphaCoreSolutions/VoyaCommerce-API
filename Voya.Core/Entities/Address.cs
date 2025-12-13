namespace Voya.Core.Entities;

public class Address
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid UserId { get; set; }
	public string Street { get; set; } = string.Empty;
	public string City { get; set; } = string.Empty;
	public string State { get; set; } = string.Empty;
	public string ZipCode { get; set; } = string.Empty;
	public string Country { get; set; } = string.Empty;
	public string PhoneNumber { get; set; } = string.Empty;
}