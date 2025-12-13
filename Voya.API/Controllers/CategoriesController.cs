using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.Application.DTOs;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers;

[ApiController]
[Route("api/v1/categories")]
public class CategoriesController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public CategoriesController(VoyaDbContext context)
	{
		_context = context;
	}

	[HttpGet]
	public async Task<ActionResult<List<CategoryDto>>> GetCategories()
	{
		// Fetch all categories from DB
		var allCategories = await _context.Categories.ToListAsync();

		// Filter root categories (those without parents)
		var rootCategories = allCategories.Where(c => c.ParentId == null).ToList();

		// Convert to DTOs recursively
		var response = rootCategories.Select(c => MapToDto(c, allCategories)).ToList();

		return Ok(response);
	}

	// Helper method to build the tree
	private CategoryDto MapToDto(Category cat, List<Category> all)
	{
		return new CategoryDto(
			cat.Id,
			cat.Name,
			cat.IconUrl,
			all.Where(sub => sub.ParentId == cat.Id)
			   .Select(sub => MapToDto(sub, all))
			   .ToList()
		);
	}
}