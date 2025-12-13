namespace Voya.Application.DTOs;

public record CartDto(
	Guid Id,
	List<CartItemDto> Items,
	decimal TotalAmount
);

public record CartItemDto(
	Guid ItemId,
	Guid ProductId,
	string ProductName,
	string MainImage,
	decimal UnitPrice,
	int Quantity,
	string SelectedOptions,
	decimal LineTotal
);

public record AddToCartRequest(Guid ProductId, int Quantity, Dictionary<string, string> Options);
public record UpdateCartItemRequest(int Quantity);