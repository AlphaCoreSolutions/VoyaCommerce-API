namespace Voya.Application.DTOs.Nexus;

public class NexusAiOverviewDto
{
	public NexusAiHealthDto Health { get; set; } = new();
	public List<NexusAiFlagDto> Flags { get; set; } = new();
	public List<NexusAiJobListItemDto> RecentJobs { get; set; } = new();
}

public class NexusAiHealthDto
{
	public string Api { get; set; } = "ok";
	public string Database { get; set; } = "unknown";
	public string Storage { get; set; } = "unknown";
	public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}

public class NexusAiFlagDto
{
	public Guid Id { get; set; }
	public string Key { get; set; } = "";
	public bool IsEnabled { get; set; }
	public string ConfigJson { get; set; } = "{}";
	public DateTime UpdatedAt { get; set; }
}

public class NexusUpsertAiFlagRequest
{
	public string Key { get; set; } = "";
	public bool IsEnabled { get; set; }
	public string ConfigJson { get; set; } = "{}";
}

public class NexusAiJobListItemDto
{
	public Guid Id { get; set; }
	public string JobKey { get; set; } = "";
	public string Status { get; set; } = "";
	public DateTime StartedAt { get; set; }
	public DateTime? FinishedAt { get; set; }
	public string Summary { get; set; } = "";
}

public class NexusAiJobDetailDto : NexusAiJobListItemDto
{
	public string Logs { get; set; } = "";
}
