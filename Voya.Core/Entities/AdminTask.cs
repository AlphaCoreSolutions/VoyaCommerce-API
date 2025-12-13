namespace Voya.Core.Entities;

public class AdminTask
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Title { get; set; } = string.Empty;
	public Guid AssignedToAdminId { get; set; } // Null = Unassigned
	public bool IsCompleted { get; set; } = false;
	public DateTime DueDate { get; set; }
}