namespace Voya.Application.DTOs;

public record CategoryDto(
	Guid Id,
	string Name,
	string? IconUrl,
	List<CategoryDto> SubCategories
);