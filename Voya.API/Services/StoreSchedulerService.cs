using Voya.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class StoreSchedulerService : BackgroundService
{
	private readonly IServiceProvider _services;
	private readonly ILogger<StoreSchedulerService> _logger;

	public StoreSchedulerService(IServiceProvider services, ILogger<StoreSchedulerService> logger)
	{
		_services = services;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			var now = DateTime.UtcNow.TimeOfDay;
			var midnight = new TimeSpan(0, 0, 0);

			using (var scope = _services.CreateScope())
			{
				var context = scope.ServiceProvider.GetRequiredService<VoyaDbContext>();

				// LOGIC: Find stores that should be closed but are open
				var storesToClose = await context.Stores
					.Where(s =>
						s.IsCurrentlyOpen &&
						s.AutoCloseEnabled &&  // User opted in
						s.AdminForceAutoClose && // Admin allowed it
												 // Logic: If current time is past closing time
												 // (Simplified for 12AM example: if it's 00:01 - 06:00, close them)
						(now >= s.CloseTime && now < s.OpenTime)
					)
					.ToListAsync(stoppingToken);

				if (storesToClose.Any())
				{
					foreach (var store in storesToClose)
					{
						store.IsCurrentlyOpen = false;
						_logger.LogInformation($"[Scheduler] Auto-closing store: {store.Name}");
					}
					await context.SaveChangesAsync(stoppingToken);
				}
			}

			// Run check every 5 minutes
			await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
		}
	}
}