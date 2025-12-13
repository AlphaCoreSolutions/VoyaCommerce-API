namespace Voya.Core.Entities;

public class NpsScore
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid UserId { get; set; }
	public int Score { get; set; } // 0-10
	public string Comment { get; set; } = string.Empty;
	public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}