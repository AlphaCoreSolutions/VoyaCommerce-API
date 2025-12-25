namespace Voya.Application.DTOs.Nexus;

public class NexusReturnListItemDto
{
	public Guid Id { get; set; }
	public string Status { get; set; } = string.Empty;

	public Guid OrderId { get; set; }
	public decimal OrderTotal { get; set; }
	public DateTime RequestedAt { get; set; }

	public Guid UserId { get; set; }
	public string UserName { get; set; } = string.Empty;
	public string UserEmail { get; set; } = string.Empty;

	public string Reason { get; set; } = string.Empty;
	public bool Restock { get; set; }
}

public class NexusReturnDetailDto
{
	public Guid Id { get; set; }
	public string Status { get; set; } = string.Empty;

	public Guid OrderId { get; set; }
	public DateTime RequestedAt { get; set; }

	public Guid UserId { get; set; }
	public string UserName { get; set; } = string.Empty;
	public string UserEmail { get; set; } = string.Empty;

	public string Reason { get; set; } = string.Empty;
	public string EvidenceUrlsJson { get; set; } = "[]";

	public string InspectionNote { get; set; } = string.Empty;
	public bool Restock { get; set; }
	public string? RejectionReason { get; set; }
}

public class NexusInspectReturnRequest
{
	public string InspectionNote { get; set; } = string.Empty;
	public bool Restock { get; set; }
}
