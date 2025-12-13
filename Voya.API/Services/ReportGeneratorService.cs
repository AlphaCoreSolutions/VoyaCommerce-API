using Microsoft.EntityFrameworkCore;
using System.Text;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

public class ReportGeneratorService : BackgroundService
{
	private readonly IServiceProvider _services;
	private readonly ILogger<ReportGeneratorService> _logger;

	public ReportGeneratorService(IServiceProvider services, ILogger<ReportGeneratorService> logger)
	{
		_services = services;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				using (var scope = _services.CreateScope())
				{
					var context = scope.ServiceProvider.GetRequiredService<VoyaDbContext>();
					var now = DateTime.UtcNow;

					// Find reports due (Daily reports not sent in last 24h)
					var schedules = await context.ReportSchedules
						.Where(s => s.LastSent < now.AddDays(-1)) // Simplified logic
						.ToListAsync(stoppingToken);

					foreach (var schedule in schedules)
					{
						// 1. GENERATE DATA (Real DB Queries)
						var sb = new StringBuilder();
						sb.AppendLine("OrderId,Date,Amount,Status");

						var orders = await context.Orders
							.Where(o => o.PlacedAt > now.AddDays(-1)) // Last 24h
							.ToListAsync(stoppingToken);

						foreach (var o in orders)
						{
							sb.AppendLine($"{o.Id},{o.PlacedAt},{o.TotalAmount},{o.Status}");
						}

						// 2. SAVE REPORT
						var report = new GeneratedReport
						{
							ReportName = $"{schedule.ReportName} - {now:yyyy-MM-dd}",
							Content = sb.ToString()
						};
						context.GeneratedReports.Add(report);

						// 3. UPDATE SCHEDULE
						schedule.LastSent = now;

						_logger.LogInformation($"Generated report: {schedule.ReportName}");
					}

					await context.SaveChangesAsync(stoppingToken);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error generating reports");
			}

			// Check every hour
			await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
		}
	}
}