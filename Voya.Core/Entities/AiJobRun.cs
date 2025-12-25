namespace Voya.Core.Entities;

public class AiJobRun
{
	public Guid Id { get; set; } = Guid.NewGuid();

	// e.g. "inventory_predictions", "daily_health_check"
	public string JobKey { get; set; } = string.Empty;

	// queued/running/success/failed
	public string Status { get; set; } = "queued";

	public DateTime StartedAt { get; set; } = DateTime.UtcNow;
	public DateTime? FinishedAt { get; set; }

	public string Summary { get; set; } = string.Empty;

	// store logs short, or store url to logs
	public string Logs { get; set; } = string.Empty;

	public Guid? TriggeredByUserId { get; set; }
}
