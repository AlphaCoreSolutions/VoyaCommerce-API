using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.API.Attributes;
using Voya.Core.Constants;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/categories")]
public class NexusCategoriesController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public NexusCategoriesController(VoyaDbContext context) { _context = context; }

	[HttpGet]
	[RequirePermission(Permissions.ContentManage)]
	public async Task<IActionResult> GetCategories()
	{
		var list = await _context.Categories
			.OrderBy(c => c.DisplayOrder)
			.ToListAsync();
		return Ok(list);
	}

	[HttpPost]
	[RequirePermission(Permissions.ContentManage)]
	public async Task<IActionResult> CreateCategory([FromBody] Category category)
	{
		category.Id = Guid.NewGuid();
		_context.Categories.Add(category);
		await _context.SaveChangesAsync();
		return Ok(category);
	}

	[HttpPut("{id}")]
	[RequirePermission(Permissions.ContentManage)]
	public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] Category category)
	{
		var existing = await _context.Categories.FindAsync(id);
		if (existing == null) return NotFound();

		existing.Name = category.Name;
		existing.IsActive = category.IsActive;
		existing.Icon = category.Icon;

		await _context.SaveChangesAsync();
		return Ok(existing);
	}

	[HttpDelete("{id}")]
	[RequirePermission(Permissions.ContentManage)]
	public async Task<IActionResult> DeleteCategory(Guid id)
	{
		var category = await _context.Categories.FindAsync(id);
		if (category == null) return NotFound();
		_context.Categories.Remove(category);
		await _context.SaveChangesAsync();
		return Ok("Category deleted.");
	}
}