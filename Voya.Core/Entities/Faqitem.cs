namespace Voya.Core.Entities;

public class FaqItem
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Question { get; set; } = string.Empty;
	public string Answer { get; set; } = string.Empty;
	public int DisplayOrder { get; set; }
	public bool IsActive { get; set; } = true;
}