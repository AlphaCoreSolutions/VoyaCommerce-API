using System.Security.Claims;
using System.Text.Json;
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

	private Guid GetUserId()
	{
		var idClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
		if (idClaim == null) throw new UnauthorizedAccessException();
		return Guid.Parse(idClaim.Value);
	}

	// === 1. GET CART (Handles Solo & Group) ===
	[HttpGet]
	public async Task<ActionResult<CartDto>> GetCart()
	{
		var userId = GetUserId();

		// Find a cart where user is Owner OR a Member
		var cart = await _context.Carts
			.Include(c => c.Items).ThenInclude(i => i.Product)
			.Include(c => c.Members).ThenInclude(m => m.User) // Include user details for avatars
			.Where(c => c.IsActive && (c.UserId == userId || c.Members.Any(m => m.UserId == userId)))
			.OrderByDescending(c => c.LastUpdated) // Get most recent
			.FirstOrDefaultAsync();

		if (cart == null)
		{
			// Create Solo Cart if none exists
			cart = new Cart { UserId = userId, Type = CartType.Solo };
			_context.Carts.Add(cart);
			await _context.SaveChangesAsync();
		}

		var amIManager = cart.UserId == userId;

		return new CartDto(
			cart.Id,
			cart.Items.Select(i => new CartItemDto(
				i.Id, i.ProductId, i.Product?.Name ?? "Unknown", i.Product?.MainImageUrl ?? "",
				i.Product?.BasePrice ?? 0, i.Quantity, i.SelectedOptionsJson,
				(i.Product?.BasePrice ?? 0) * i.Quantity,
				i.AddedByUserId, i.AddedByName
			)).ToList(),
			cart.Items.Sum(i => (i.Product?.BasePrice ?? 0) * i.Quantity),
			cart.Type.ToString(),
			cart.SharingToken,
			cart.Members.Select(m => new CartMemberDto(m.UserId, m.User.FullName, m.User.AvatarUrl ?? "", m.Role)).ToList(),
			amIManager
		);
	}

	// === 2. ADD TO CART (Updated for Group) ===
	[HttpPost("add")]
	public async Task<IActionResult> AddToCart(AddToCartRequest request)
	{
		var userId = GetUserId();
		var user = await _context.Users.FindAsync(userId);
		var cart = await GetActiveCart(userId); // Helper method below

		// ... (Validation logic same as before) ...
		var product = await _context.Products.FindAsync(request.ProductId);
		if (product == null || product.StockQuantity < request.Quantity) return BadRequest("Stock issue");

		var optionsJson = JsonSerializer.Serialize(request.Options ?? new Dictionary<string, string>());

		// Check if item exists (Same Product + Same Options + Same AddedBy User)
		// Note: In shared cart, if I add Item A, and You add Item A, they are separate rows usually
		// unless you want them merged. Merging is cleaner for checkout.
		var existingItem = cart.Items.FirstOrDefault(i =>
			i.ProductId == request.ProductId &&
			i.SelectedOptionsJson == optionsJson &&
			i.AddedByUserId == userId); // Only merge my own adds

		if (existingItem != null)
		{
			existingItem.Quantity += request.Quantity;
		}
		else
		{
			cart.Items.Add(new CartItem
			{
				CartId = cart.Id,
				ProductId = request.ProductId,
				Quantity = request.Quantity,
				SelectedOptionsJson = optionsJson,
				AddedByUserId = userId,
				AddedByName = user?.FullName ?? "Member"
			});
		}

		cart.LastUpdated = DateTime.UtcNow;
		await _context.SaveChangesAsync();
		return Ok("Added");
	}

	// === 3. REMOVE (With Permission Check) ===
	[HttpDelete("{itemId}")]
	public async Task<IActionResult> RemoveItem(Guid itemId)
	{
		var userId = GetUserId();
		var cart = await GetActiveCart(userId);
		var item = cart.Items.FirstOrDefault(i => i.Id == itemId);

		if (item == null) return NotFound();

		// Rule: Manager can delete anything. Member can only delete their own.
		bool isManager = cart.UserId == userId;
		if (!isManager && item.AddedByUserId != userId)
		{
			return StatusCode(403, "You can only remove items you added.");
		}

		_context.CartItems.Remove(item);
		await _context.SaveChangesAsync();
		return Ok();
	}

	// === 4. CREATE/CONVERT TO GROUP CART ===
	[HttpPost("group/create")]
	public async Task<IActionResult> CreateGroupCart()
	{
		var userId = GetUserId();
		var cart = await GetActiveCart(userId);

		if (cart.Type == CartType.Group)
			return Ok(new { Token = cart.SharingToken }); // Already group

		// Convert Solo to Group
		cart.Type = CartType.Group;
		cart.SharingToken = GenerateToken(); // Simple 6-char token

		// Add owner as first member (Manager)
		_context.CartMembers.Add(new CartMember
		{
			CartId = cart.Id,
			UserId = userId,
			Role = "Manager"
		});

		await _context.SaveChangesAsync();
		return Ok(new { Token = cart.SharingToken });
	}

	// === 5. JOIN GROUP CART ===
	[HttpPost("group/join")]
	public async Task<IActionResult> JoinGroupCart([FromBody] JoinCartRequest request)
	{
		var userId = GetUserId();
		var cart = await _context.Carts
			.Include(c => c.Members)
			.FirstOrDefaultAsync(c => c.SharingToken == request.Token && c.IsActive);

		if (cart == null) return NotFound("Invalid or expired token.");

		if (cart.Members.Any(m => m.UserId == userId) || cart.UserId == userId)
			return Ok("Already a member.");

		// Add User
		_context.CartMembers.Add(new CartMember
		{
			CartId = cart.Id,
			UserId = userId,
			Role = "Member"
		});

		// If user had a solo cart, maybe archive it? For now, we just switch context.
		// We could also merge items from solo cart to group cart here.

		await _context.SaveChangesAsync();
		return Ok(new { CartId = cart.Id, Message = "Joined successfully" });
	}

	// Helpers
	private async Task<Cart> GetActiveCart(Guid userId)
	{
		var cart = await _context.Carts
			.Include(c => c.Items)
			.Where(c => c.IsActive && (c.UserId == userId || c.Members.Any(m => m.UserId == userId)))
			.OrderByDescending(c => c.LastUpdated)
			.FirstOrDefaultAsync();

		if (cart == null)
		{
			// Should usually allow AddToCart to create one, but for safety:
			cart = new Cart { UserId = userId, Type = CartType.Solo };
			_context.Carts.Add(cart);
			await _context.SaveChangesAsync();
		}
		return cart;
	}

	private string GenerateToken()
	{
		const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
		return new string(Enumerable.Repeat(chars, 6)
			.Select(s => s[new Random().Next(s.Length)]).ToArray());
	}
}