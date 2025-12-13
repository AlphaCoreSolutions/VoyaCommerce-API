namespace Voya.Core.Entities;

public enum FlashSaleStatus { Draft, PendingApproval, Active, Ended, Rejected }

public class FlashSale
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid StoreId { get; set; }

	public string Name { get; set; } = string.Empty;
	public decimal DiscountPercentage { get; set; }
	public DateTime StartTime { get; set; }
	public DateTime EndTime { get; set; }

	public FlashSaleStatus Status { get; set; } = FlashSaleStatus.Draft;

	// Products included in this sale
	public List<Guid> ProductIds { get; set; } = new();
}