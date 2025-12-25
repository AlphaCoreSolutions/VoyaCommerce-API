namespace Voya.Application.DTOs.Nexus;

public class NexusGlobalOverviewDto
{
	public NexusGlobalTotalsDto Totals { get; set; } = new();
}

public class NexusGlobalTotalsDto
{
	public int TotalUsers { get; set; }
	public int TotalStores { get; set; }
	public int TotalOrders { get; set; }
	public decimal TotalRevenue { get; set; }

	public int OrdersLast7Days { get; set; }
	public decimal RevenueLast7Days { get; set; }
}



