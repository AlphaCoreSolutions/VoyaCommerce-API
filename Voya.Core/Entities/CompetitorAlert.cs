namespace Voya.Core.Entities;

public class CompetitorAlert
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid StoreId { get; set; }
	public Guid ProductId { get; set; }

	public string CompetitorName { get; set; } = string.Empty; // e.g., "Amazon"
	public decimal CompetitorPrice { get; set; }
	public decimal MyPrice { get; set; }

	public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
	public bool IsResolved { get; set; } = false; // Seller dismissed alert
}