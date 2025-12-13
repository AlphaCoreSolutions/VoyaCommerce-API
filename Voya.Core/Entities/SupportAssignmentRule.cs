namespace Voya.Core.Entities;

public class SupportAssignmentRule
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Keyword { get; set; } = string.Empty; // e.g. "Billing"
	public Guid TargetDepartmentRoleId { get; set; } // Assign to "Finance" Role
	public int Priority { get; set; } = 0;
}