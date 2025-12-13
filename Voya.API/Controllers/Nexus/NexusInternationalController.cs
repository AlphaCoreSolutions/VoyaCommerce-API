using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.Nexus;

[Authorize]
[ApiController]
[Route("api/v1/nexus/international")]
public class NexusInternationalController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public NexusInternationalController(VoyaDbContext context) { _context = context; }

	// FEATURE 9: MULTI-CURRENCY & COUNTRIES
	[HttpGet("currencies")]
	public async Task<IActionResult> GetCurrencies() => Ok(await _context.Currencies.ToListAsync());

	[HttpPost("currencies")]
	public async Task<IActionResult> AddCurrency([FromBody] Currency currency)
	{
		_context.Currencies.Add(currency);
		await _context.SaveChangesAsync();
		return Ok("Currency added.");
	}

	[HttpGet("countries")]
	public async Task<IActionResult> GetCountries() => Ok(await _context.Countries.Include(c => c.Currency).ToListAsync());

	[HttpPost("countries")]
	public async Task<IActionResult> AddCountry([FromBody] Country country)
	{
		_context.Countries.Add(country);
		await _context.SaveChangesAsync();
		return Ok("Country added.");
	}
}