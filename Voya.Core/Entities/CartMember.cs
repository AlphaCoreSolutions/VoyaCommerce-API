namespace Voya.Core.Entities;

public class CartMember
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public Guid CartId { get; set; }
	public Cart Cart { get; set; } = null!;

	public Guid UserId { get; set; }
	public User User { get; set; } = null!; // Assuming you have a User entity

	public string Role { get; set; } = "Member"; // "Manager" or "Member"
	public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}