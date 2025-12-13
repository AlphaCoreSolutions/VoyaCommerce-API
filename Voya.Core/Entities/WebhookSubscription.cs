namespace Voya.Core.Entities;

public class WebhookSubscription
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Url { get; set; } = string.Empty;
	public string EventType { get; set; } = "order.created"; // "order.shipped", etc.
	public string Secret { get; set; } = Guid.NewGuid().ToString(); // For verifying signature
	public bool IsActive { get; set; } = true;
}