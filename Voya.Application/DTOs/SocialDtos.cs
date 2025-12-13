namespace Voya.Application.DTOs;

// Reviews
public record AddReviewRequest(
	Guid OrderId,    // Changed from string
	Guid ProductId,  // Changed from string
	int Rating,
	string Comment,
	List<string> Images
);

// Update PendingReviewDto to accept Guid or convert inside controller
public record PendingReviewDto(
	Guid ProductId,
	string ProductName,
	string ProductImage,
	string OrderId, // Keep as string for UI if preferred, or change to Guid
	string Date
);
public record ReviewDto(Guid Id, string UserName, int Rating, string Comment, List<string> Images, string Date);

// Wishlist
public record WishlistDto(Guid ProductId, string Name, decimal Price, string ImageUrl);

// Notification
public record NotificationDto(Guid Id, string Title, string Body, bool IsRead, string Date);