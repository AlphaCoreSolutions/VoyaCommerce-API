namespace Voya.Application.DTOs;

public record CartDto(
	Guid Id,
	List<CartItemDto> Items,
	decimal TotalAmount,
	string Type,           // "Solo" or "Group"
	string? SharingToken,  // Null if solo
	List<CartMemberDto> Members,
	bool AmIManager        // Helper for frontend logic
);

public record CartItemDto(
	Guid Id,
	Guid ProductId,
	string ProductName,
	string ImageUrl,
	decimal Price,
	int Quantity,
	string Options,
	decimal LineTotal,
	Guid AddedByUserId,    // NEW
	string AddedByName     // NEW
);

public record CartMemberDto(
	Guid UserId,
	string FullName,
	string AvatarUrl,
	string Role
);

public record JoinCartRequest(string Token);

public record AddToCartRequest(Guid ProductId, int Quantity, Dictionary<string, string> Options);
public record UpdateCartItemRequest(int Quantity);