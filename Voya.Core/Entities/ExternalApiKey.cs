namespace Voya.Core.Entities;

public class ExternalApiKey
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string PartnerName { get; set; } = string.Empty; // "Marketing Agency X"
	public string ApiKeyHash { get; set; } = string.Empty; // Never store plain text
	public string PermissionsJson { get; set; } = "[]"; // ["products.read", "orders.read"]
	public DateTime ExpiresAt { get; set; }
	public bool IsActive { get; set; } = true;
}