namespace Voya.Core.Entities;

public class AbTestExperiment
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Name { get; set; } = string.Empty; // "New Checkout Flow"
	public string VariantA_Name { get; set; } = "Control";
	public string VariantB_Name { get; set; } = "Variation";

	// Percentage of users who see Variant B (e.g., 50%)
	public int TrafficAllocationPercent { get; set; } = 50;

	public bool IsActive { get; set; } = false;
}