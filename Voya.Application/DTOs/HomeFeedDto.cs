namespace Voya.Application.DTOs;

public record HomeFeedDto(
	List<ProductListDto> FlashSales,
	List<ProductListDto> Highlights,
	List<string> Banners, // URLs for banner images
	DateTime FlashSaleEndTime
);