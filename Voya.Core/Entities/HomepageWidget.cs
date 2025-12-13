namespace Voya.Core.Entities;

public class HomepageWidget
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Title { get; set; } = string.Empty; // "New Arrivals"
	public string Type { get; set; } = "ProductList"; // "Banner", "CategoryGrid"
	public string DataSourceUrl { get; set; } = string.Empty; // API endpoint to fetch data
	public int SortOrder { get; set; }
	public bool IsActive { get; set; } = true;
}