public class CompetitorScraperService : BackgroundService
{
	// Scans internet for prices. 
	// Logic: 
	// 1. Get List of Products with "EAN/UPC" codes.
	// 2. Query Google Shopping API (mocked).
	// 3. If Price < MyPrice, create CompetitorAlert in DB.
	protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
}