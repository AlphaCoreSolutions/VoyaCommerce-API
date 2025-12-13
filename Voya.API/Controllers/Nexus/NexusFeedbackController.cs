using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Nexus;

[ApiController]
[Route("api/v1/nexus/feedback")]
public class NexusFeedbackController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public NexusFeedbackController(VoyaDbContext context) { _context = context; }

	// FEATURE 8: FEEDBACK / NPS ANALYZER
	[HttpPost("submit")]
	[AllowAnonymous]
	public async Task<IActionResult> SubmitScore([FromBody] NpsScore score)
	{
		_context.NpsScores.Add(score);
		await _context.SaveChangesAsync();
		return Ok("Thank you for your feedback!");
	}

	[HttpGet("analysis")]
	[Authorize]
	public async Task<IActionResult> GetNpsAnalysis()
	{
		var scores = await _context.NpsScores.ToListAsync();
		if (!scores.Any()) return Ok(new { Average = 0 });

		var average = scores.Average(s => s.Score);
		var promoters = scores.Count(s => s.Score >= 9);
		var detractors = scores.Count(s => s.Score <= 6);

		return Ok(new
		{
			AverageScore = average,
			TotalResponses = scores.Count,
			Promoters = promoters,
			Detractors = detractors
		});
	}
}