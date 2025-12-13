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
[Route("api/v1/payment-methods")]
public class PaymentController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public PaymentController(VoyaDbContext context)
	{
		_context = context;
	}

	private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")!.Value);

	[HttpGet]
	public async Task<ActionResult<List<PaymentMethodDto>>> GetMyMethods()
	{
		var userId = GetUserId();
		var methods = await _context.PaymentMethods
			.Where(p => p.UserId == userId)
			.Select(p => new PaymentMethodDto(p.Id, p.Type, p.DisplayName, p.IsDefault))
			.ToListAsync();
		return Ok(methods);
	}

	[HttpPost]
	public async Task<IActionResult> AddPaymentMethod(CreatePaymentMethodRequest request)
	{
		var userId = GetUserId();

		// SIMULATION: In reality, you send card details to Stripe/PayPal and get a token back.
		// We will simulate that "Tokenization" here.
		var last4 = request.CardNumber.Length >= 4 ? request.CardNumber[^4..] : "0000";
		var cardType = request.CardNumber.StartsWith("4") ? "Visa" : "MasterCard";

		var method = new PaymentMethod
		{
			UserId = userId,
			Type = cardType,
			DisplayName = $"{cardType} ending in {last4}",
			ProviderToken = $"tok_{Guid.NewGuid()}", // Fake token
			IsDefault = !await _context.PaymentMethods.AnyAsync(p => p.UserId == userId) // First card is default
		};

		_context.PaymentMethods.Add(method);
		await _context.SaveChangesAsync();
		return Ok(new { Message = "Payment Method added", Id = method.Id });
	}
}