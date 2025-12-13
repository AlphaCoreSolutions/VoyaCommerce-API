using Voya.Core.Enums;

namespace Voya.Core.Entities;

public class Store
{
	public Guid Id { get; set; } = Guid.NewGuid();

	// Link to the User (Owner)
	public Guid OwnerId { get; set; }
	public User Owner { get; set; } = null!;

	// Basic Info
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public string? LogoUrl { get; set; }
	public string? CoverImageUrl { get; set; }

	// Legal & Contact (Crucial for the "Contract" phase)
	public string BusinessEmail { get; set; } = string.Empty;
	public string PhoneNumber { get; set; } = string.Empty;
	public string TaxNumber { get; set; } = string.Empty; // e.g., VAT ID
	public string Address { get; set; } = string.Empty;
	public string City { get; set; } = string.Empty;

	// Workflow State
	public StoreStatus Status { get; set; } = StoreStatus.Draft;
	public string? AdminNotes { get; set; } // e.g., "Please re-upload clear ID"

	// Metrics (For Dashboard)
	public decimal TotalRevenue { get; set; } = 0;
	public int TotalSales { get; set; } = 0;
	public double Rating { get; set; } = 5.0;

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public DateTime? ActivatedAt { get; set; }

	// Feature 8: Vacation Mode
	public bool IsOnVacation { get; set; } = false;
	public string? VacationMessage { get; set; } // "Back on Monday!"

	// Feature 8 (Advanced): Auto-Close Logic
	public bool AutoCloseEnabled { get; set; } = false; // "Opt-in" request status
	public bool AdminForceAutoClose { get; set; } = true; // Admin master switch (default True)

	// Operating Hours (UTC)
	public TimeSpan OpenTime { get; set; } = new TimeSpan(8, 0, 0);  // 8:00 AM
	public TimeSpan CloseTime { get; set; } = new TimeSpan(0, 0, 0); // 12:00 AM (Midnight)

	public bool IsCurrentlyOpen { get; set; } = true; // The actual switch the app checks

	// Navigation
	public ICollection<Product> Products { get; set; } = new List<Product>();

	public decimal DeliveryBaseFee { get; set; } = 2.00m;
	public decimal DeliveryFeePerKm { get; set; } = 0.50m; // Charge 0.50 per KM

	public bool IsVerified { get; set; } = false;
	public bool IsBoosted { get; set; } = false; // Hero Section
	public decimal CommissionRate { get; set; } = 10.0m; // Default 10%

}