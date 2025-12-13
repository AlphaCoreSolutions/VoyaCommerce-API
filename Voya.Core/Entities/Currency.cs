namespace Voya.Core.Entities;

public class Currency
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Code { get; set; } = "USD";
	public string Name { get; set; } = "US Dollar";
	public string Symbol { get; set; } = "$";
	public decimal ExchangeRate { get; set; } = 1.0m; // Relative to Base Currency (JOD)
	public bool IsActive { get; set; } = true;
}

