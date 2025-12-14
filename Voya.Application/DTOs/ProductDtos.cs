namespace Voya.Application.DTOs;

public record ProductDto(
	Guid Id,
	string Name,
	string Description,
	decimal BasePrice,
	decimal? DiscountPrice,
	int StockQuantity,
	string MainImageUrl,
	List<string> GalleryImages,
	string CategoryName,
	List<ProductOptionDto> Options,
	List<string> Tags,

	// --- ADD THESE FIELDS ---
	Guid StoreId,
	string SellerName,
	string SellerAvatar,
	double SellerRating
);

public record ProductOptionDto(string Name, List<ProductOptionValueDto> Values);
public record ProductOptionValueDto(string Label, decimal PriceModifier);

public record ProductListDto(
	Guid Id,
	string Name,
	decimal Price,
	decimal? DiscountPrice,
	string MainImageUrl,
	double Rating,
	Guid StoreId,
	string SellerName,
	string SellerAvatar,
	double SellerRating
);