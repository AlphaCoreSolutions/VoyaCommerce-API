using Microsoft.EntityFrameworkCore;
using Voya.Infrastructure.Persistence;

public class AbandonedCartService : BackgroundService
{
	private readonly IServiceProvider _services;
	private readonly ILogger<AbandonedCartService> _logger; // Use Logger instead of Console

	public AbandonedCartService(IServiceProvider services, ILogger<AbandonedCartService> logger)
	{
		_services = services;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Abandoned Cart Service started.");

		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				using (var scope = _services.CreateScope())
				{
					var context = scope.ServiceProvider.GetRequiredService<VoyaDbContext>();
					var cutoff = DateTime.UtcNow.AddHours(-24);

					var lostCarts = await context.Carts
						.Include(c => c.Items)
						.Where(c => c.LastUpdated < cutoff && c.Items.Any())
						.ToListAsync(stoppingToken);

					if (lostCarts.Any())
					{
						foreach (var cart in lostCarts)
						{
							// Mock Email Logic
							_logger.LogInformation($"[Reminder] User {cart.UserId} has {cart.Items.Count} items in cart.");

							// Update timestamp to avoid spamming
							cart.LastUpdated = DateTime.UtcNow;
						}

						await context.SaveChangesAsync(stoppingToken);
					}
				}
			}
			catch (OperationCanceledException)
			{
				// Graceful shutdown, do nothing
				break;
			}
			catch (Exception ex)
			{
				// Log error but keep the service alive for next hour
				_logger.LogError(ex, "Error processing abandoned carts. Retrying in 1 hour.");
			}

			// Wait 1 hour before next run
			await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
		}
	}
}