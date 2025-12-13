namespace Voya.Core.Entities;

public enum LiveEventStatus { Scheduled, Live, Ended, Cancelled }

public class LiveEvent
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid StoreId { get; set; }

	public string Title { get; set; } = string.Empty;
	public DateTime ScheduledAt { get; set; }
	public LiveEventStatus Status { get; set; } = LiveEventStatus.Scheduled;

	// Future-proofing fields
	public string? StreamKey { get; set; }
	public string? ThumbnailUrl { get; set; }
	public List<Guid> FeaturedProductIds { get; set; } = new();
}