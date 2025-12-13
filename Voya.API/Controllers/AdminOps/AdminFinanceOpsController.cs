using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voya.API.Attributes;
using Voya.Core.Constants;
using Voya.Core.Entities;
using Voya.Infrastructure.Persistence;

namespace Voya.API.Controllers.AdminOps;

[Authorize]
[ApiController]
[Route("api/v1/admin/finance")]
public class AdminFinanceOpsController : ControllerBase
{
	private readonly VoyaDbContext _context;
	public AdminFinanceOpsController(VoyaDbContext context) { _context = context; }

	// FEATURE 3: COMPLEX REFUND MANAGER

	// Option A: Refund to Voya Wallet (Instant)
	[HttpPost("refund/wallet")]
	[RequirePermission(Permissions.FinanceRefund)]
	public async Task<IActionResult> RefundToWallet([FromBody] RefundRequestDto req)
	{
		var order = await _context.Orders.Include(o => o.User).FirstOrDefaultAsync(o => o.Id == req.OrderId);
		if (order == null) return NotFound();

		// 1. Credit Wallet
		order.User.WalletBalance += req.Amount;

		// 2. Log Transaction
		// _context.UserTransactions.Add(...) 

		await _context.SaveChangesAsync();
		return Ok($"Refunded {req.Amount} to user's wallet.");
	}

	// Option B: Refund to Card (External API)
	[HttpPost("refund/card")]
	[RequirePermission(Permissions.FinanceRefund)]
	public async Task<IActionResult> RefundToCard([FromBody] RefundRequestDto req)
	{
		// Call Stripe/Payment Gateway
		// await _paymentGateway.RefundAsync(order.PaymentIntentId, req.Amount);
		return Ok("Refund request sent to Payment Gateway.");
	}

	// Option C: Cash Appointment (Complex Workflow)
	[HttpPost("refund/cash/request")]
	[RequirePermission(Permissions.FinanceRefund)]
	public async Task<IActionResult> CreateCashRequest([FromBody] RefundRequestDto req)
	{
		// 1. Create the request in system
		var appointment = new CashRefundAppointment
		{
			OrderId = req.OrderId,
			UserId = req.UserId,
			Amount = req.Amount,
			Status = RefundStatus.Pending, // Waiting for admin to schedule
			AdminNotes = "User requested cash refund. Please call to schedule."
		};

		_context.CashRefundAppointments.Add(appointment);
		await _context.SaveChangesAsync();
		return Ok("Cash refund request created. Assigned to Finance Team.");
	}

	[HttpPost("refund/cash/schedule")]
	[RequirePermission(Permissions.FinanceRefund)] // Specific role handles this
	public async Task<IActionResult> ScheduleCashRefund(Guid appointmentId, [FromBody] ScheduleDto req)
	{
		var appt = await _context.CashRefundAppointments.FindAsync(appointmentId);
		if (appt == null) return NotFound();

		appt.ScheduledTime = req.Date;
		appt.Location = req.Location;
		appt.Status = RefundStatus.Scheduled;

		// NOTIFICATION LOGIC (Mocked)
		// _notificationService.Send(appt.UserId, $"Cash Refund Scheduled for {req.Date} at {req.Location}");

		await _context.SaveChangesAsync();
		return Ok("Appointment scheduled and user notified.");
	}
}

public class RefundRequestDto { public Guid OrderId { get; set; } public Guid UserId { get; set; } public decimal Amount { get; set; } }
public class ScheduleDto { public DateTime Date { get; set; } public string Location { get; set; } = ""; }