using System.Security.Claims;
using System.Text.Json; // Required for JSON serialization
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.Application.DTOs;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/cart")]
public class CartController : ControllerBase
{
	private readonly VoyaDbContext _context;

	public CartController(VoyaDbContext context)
	{
		_context = context;
	}

	// === FIX 1: Safer User ID Retrieval ===
	// Sometimes "sub" and "NameIdentifier" mapping gets mixed up in .NET. This checks both.
	private Guid GetUserId()
	{
		var idClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");

		if (idClaim == null)
			throw new UnauthorizedAccessException("Invalid Token: No User ID found.");

		return Guid.Parse(idClaim.Value);
	}

	[HttpGet]
	public async Task<ActionResult<CartDto>> GetCart()
	{
		try
		{
			var userId = GetUserId();
			var cart = await GetOrCreateCart(userId);

			var cartDto = new CartDto(
				cart.Id,
				cart.Items.Select(i => new CartItemDto(
					i.Id,
					i.ProductId,
					i.Product?.Name ?? "Unknown Product", // Handle potential nulls safely
					i.Product?.MainImageUrl ?? "",
					i.Product?.BasePrice ?? 0,
					i.Quantity,
					i.SelectedOptionsJson,
					(i.Product?.BasePrice ?? 0) * i.Quantity
				)).ToList(),
				cart.Items.Sum(i => (i.Product?.BasePrice ?? 0) * i.Quantity)
			);

			return Ok(cartDto);
		}
		catch (Exception ex)
		{
			// Log the error (in a real app) and return 500
			Console.WriteLine($"Error in GetCart: {ex.Message}");
			return StatusCode(500, "Internal Server Error");
		}
	}

	[HttpPost("add")]
	public async Task<IActionResult> AddToCart(AddToCartRequest request)
	{
		try
		{
			var userId = GetUserId();
			// 1. Ensure Cart Exists (but don't rely on it for tracking the item)
			var cart = await GetOrCreateCart(userId);

			// 2. Prepare Options
			var optionsDict = request.Options ?? new Dictionary<string, string>();
			var optionsJson = JsonSerializer.Serialize(optionsDict);

			// 3. Validate Product & Stock
			var product = await _context.Products.FindAsync(request.ProductId);
			if (product == null) return NotFound("Product not found");
			if (product.StockQuantity < request.Quantity) return BadRequest("Insufficient stock");

			// 4. Check for duplicates manually using the DbSet
			// This is safer than checking cart.Items which might be stale
			var existingItem = await _context.CartItems
				.FirstOrDefaultAsync(i =>
					i.CartId == cart.Id &&
					i.ProductId == request.ProductId &&
					i.SelectedOptionsJson == optionsJson);

			if (existingItem != null)
			{
				// Update existing item
				existingItem.Quantity += request.Quantity;
				_context.CartItems.Update(existingItem);
			}
			else
			{
				// Create new item
				var newItem = new CartItem
				{
					Id = Guid.NewGuid(), // Explicitly set ID
					CartId = cart.Id,    // Link using Foreign Key
					ProductId = request.ProductId,
					Quantity = request.Quantity,
					SelectedOptionsJson = optionsJson
				};

				// Add DIRECTLY to the table, not the parent list
				_context.CartItems.Add(newItem);
			}

			// 5. Update Cart Timestamp (Optional, but good practice)
			cart.LastUpdated = DateTime.UtcNow;
			_context.Carts.Update(cart);

			await _context.SaveChangesAsync();
			return Ok("Item added");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error in AddToCart: {ex.Message}");
			return StatusCode(500, ex.Message);
		}
	}
	[HttpDelete("{itemId}")]
	public async Task<IActionResult> RemoveItem(Guid itemId)
	{
		var userId = GetUserId();
		var cart = await GetOrCreateCart(userId);

		var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
		if (item == null) return NotFound();

		_context.CartItems.Remove(item);
		await _context.SaveChangesAsync();
		return Ok();
	}

	private async Task<Cart> GetOrCreateCart(Guid userId)
	{
		var cart = await _context.Carts
			.Include(c => c.Items)
			.ThenInclude(i => i.Product)
			.FirstOrDefaultAsync(c => c.UserId == userId);

		if (cart == null)
		{
			cart = new Cart { UserId = userId };
			_context.Carts.Add(cart);
			await _context.SaveChangesAsync();
		}
		return cart;
	}
}