using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.Application.DTOs;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/addresses")]
public class AddressController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public AddressController(VoyaDbContext context)
	{
		_context = context;
	}

	private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")!.Value);

	[HttpGet]
	public async Task<ActionResult<List<AddressDto>>> GetMyAddresses()
	{
		var userId = GetUserId();
		var addresses = await _context.Addresses
			.Where(a => a.UserId == userId)
			.Select(a => new AddressDto(a.Id, a.Street, a.City, a.State, a.ZipCode, a.Country))
			.ToListAsync();
		return Ok(addresses);
	}

	[HttpPost]
	public async Task<IActionResult> AddAddress(CreateAddressRequest request)
	{
		var userId = GetUserId();
		var address = new Address
		{
			UserId = userId,
			Street = request.Street,
			City = request.City,
			State = request.State,
			ZipCode = request.ZipCode,
			Country = request.Country,
			PhoneNumber = request.PhoneNumber
		};

		_context.Addresses.Add(address);
		await _context.SaveChangesAsync();
		return Ok(new { Message = "Address saved", AddressId = address.Id });
	}
}