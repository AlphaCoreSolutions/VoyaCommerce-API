using Voya.Core.Entities;

namespace Voya.Application.Interfaces.Logistics; // <--- CHECK THIS NAMESPACE

public interface IShippingGateway
{
	Task<ShipmentResult> CreateShipmentAsync(Order order, LogisticsProvider config);
	Task<string> GetLabelUrlAsync(string trackingNumber, LogisticsProvider config);
	Task<string> GetStatusAsync(string trackingNumber, LogisticsProvider config);
}

public class ShipmentResult
{
	public string TrackingNumber { get; set; } = "";
	public string LabelUrl { get; set; } = "";
	public decimal Cost { get; set; }
}