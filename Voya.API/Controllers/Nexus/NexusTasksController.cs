using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/tasks")]
public class NexusTasksController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public NexusTasksController(VoyaDbContext context) { _context = context; }

	// FEATURE 5: ADMIN TASK MANAGER
	[HttpGet]
	public async Task<IActionResult> GetMyTasks()
	{
		// For simplicity, return all tasks. Ideally filter by logged-in admin.
		return Ok(await _context.AdminTasks.ToListAsync());
	}

	[HttpPost]
	public async Task<IActionResult> CreateTask([FromBody] AdminTask task)
	{
		_context.AdminTasks.Add(task);
		await _context.SaveChangesAsync();
		return Ok(task);
	}
}