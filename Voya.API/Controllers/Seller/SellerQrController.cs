using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.Infrastructure.Persistence;

[Authorize]
[ApiController]
[Route("api/v1/seller/qr")]
public class SellerQrController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public SellerQrController(VoyaDbContext context) { _context = context; }

	[HttpGet("order/{orderId}")]
	public async Task<IActionResult> GetOrderQrData(Guid orderId)
	{
		var order = await _context.Orders
			.Include(o => o.User)
			.FirstOrDefaultAsync(o => o.Id == orderId);

		if (order == null) return NotFound();

		// Data payload for the QR Code
		// Format: "VOYA-ORD|{Guid}|{Amount}|{Date}"
		var qrPayload = $"VOYA-ORD|{order.Id}|{order.TotalAmount}|{order.PlacedAt:yyyyMMddHHmm}";

		// Data for the 14-invoice print
		var invoiceData = new
		{
			InvoiceNumber = order.Id.ToString().Substring(0, 8).ToUpper(),
			Customer = order.User.FullName,
			ItemsCount = order.Items.Count,
			Total = order.TotalAmount,
			Date = order.PlacedAt,
			QrString = qrPayload
		};

		return Ok(invoiceData);
	}
}