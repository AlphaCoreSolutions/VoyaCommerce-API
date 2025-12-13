using Microsoft.Extensions.DependencyInjection;
using Voya.Application.Interfaces.Logistics; // <--- FIX: Finds IShippingGateway
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.Infrastructure.Services.Logistics;

public class InternalLogisticsAdapter : IShippingGateway
{
	private readonly IServiceProvider _serviceProvider;

	public InternalLogisticsAdapter(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
	}

	public async Task<ShipmentResult> CreateShipmentAsync(Order order, LogisticsProvider config)
	{
		using (var scope = _serviceProvider.CreateScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<VoyaDbContext>();

			var task = new DeliveryTask
			{
				OrderId = order.Id,
				// FIX: Use new unambiguous Enum
				Status = DeliveryTaskStatus.Assigned,
				AddressText = "See Order Address",
				CreatedAt = DateTime.UtcNow
			};

			context.DeliveryTasks.Add(task);
			await context.SaveChangesAsync();

			return new ShipmentResult
			{
				TrackingNumber = $"VOYA-{task.Id.ToString().Substring(0, 8).ToUpper()}",
				LabelUrl = $"/api/v1/logistics/internal/label/{task.Id}",
				Cost = 0
			};
		}
	}

	public Task<string> GetLabelUrlAsync(string trackingNumber, LogisticsProvider config)
	{
		return Task.FromResult($"/api/v1/logistics/internal/label/view/{trackingNumber}");
	}

	public Task<string> GetStatusAsync(string trackingNumber, LogisticsProvider config)
	{
		return Task.FromResult("Processing");
	}
}