using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Concurrent;

namespace Voya.API.Hubs;

[Authorize]
public class LiveHub : Hub
{
	// Map UserID to ConnectionID to send direct messages
	// In production, use Redis. For now, a static dictionary works for single-server.
	private static readonly ConcurrentDictionary<string, string> _onlineUsers = new();

	public override async Task OnConnectedAsync()
	{
		var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

		if (userId != null)
		{
			_onlineUsers[userId] = Context.ConnectionId;

			// Add to a personal group so we can target them easily
			await Groups.AddToGroupAsync(Context.ConnectionId, userId);

			// If they are a driver, add to "Drivers" group
			if (Context.User.IsInRole("Driver"))
			{
				await Groups.AddToGroupAsync(Context.ConnectionId, "DriversGroup");
			}
		}

		await base.OnConnectedAsync();
	}

	public override Task OnDisconnectedAsync(Exception? exception)
	{
		var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
		if (userId != null)
		{
			_onlineUsers.TryRemove(userId, out _);
		}
		return base.OnDisconnectedAsync(exception);
	}

	// --- CLIENT METHODS (Called by Flutter Apps) ---

	// 1. Driver sends location update -> Sent to Customer
	public async Task UpdateDriverLocation(string orderId, double lat, double lng)
	{
		// Broadcast to anyone listening to this specific Order (e.g., the Customer)
		await Clients.Group($"Order_{orderId}").SendAsync("ReceiveLocationUpdate", new { lat, lng });
	}

	// 2. Customer joins the "Live Tracking" room for their order
	public async Task JoinOrderTracking(string orderId)
	{
		await Groups.AddToGroupAsync(Context.ConnectionId, $"Order_{orderId}");
	}

	// 3. User sends Chat Message -> Sent to Support Agent
	public async Task SendChatMessage(string message)
	{
		var userId = Context.UserIdentifier;
		// Send to "SupportTeam" group
		await Clients.Group("SupportTeam").SendAsync("ReceiveSupportMessage", new { UserId = userId, Message = message });
	}
}