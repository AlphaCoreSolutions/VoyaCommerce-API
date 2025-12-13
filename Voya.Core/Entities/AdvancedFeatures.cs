namespace Voya.Core.Entities;

public enum ApprovalStatus { Pending, Approved, Rejected, Active, Completed, Cancelled }

// 1. Reverse Auction (Mystery Price Drop)
public class ReverseAuction
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid StoreId { get; set; }
	public Guid ProductId { get; set; }

	public decimal StartPrice { get; set; }
	public decimal MinPrice { get; set; }      // Floor price
	public decimal DecrementAmount { get; set; } // Drop $1
	public int IntervalSeconds { get; set; }   // Every 10 seconds

	public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
	public DateTime? StartTime { get; set; }   // When the drop begins
	public DateTime? EndedTime { get; set; }   // When someone bought it
	public Guid? WinnerUserId { get; set; }
}

// 2. Style Board (User Generated Content)
public class StyleBoard
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid CreatorUserId { get; set; }
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;

	// JSON list of Product IDs included in this look
	public List<Guid> ProductIds { get; set; } = new();

	public int SalesGenerated { get; set; } = 0;
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// 4. Social Gift
public class GiftOrder
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid SenderUserId { get; set; }
	public string RecipientPhoneNumber { get; set; } = string.Empty;
	public string? PersonalMessage { get; set; }

	public string OrderId { get; set; } = string.Empty; // Linked Order
	public string ClaimToken { get; set; } = string.Empty; // "GIFT-XYZ"
	public bool IsClaimed { get; set; } = false;
}

// 5. Pre-Order (Crowdfunding)
public class CrowdFundCampaign
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid StoreId { get; set; }
	public Guid ProductId { get; set; }

	public int TargetQuantity { get; set; } // Need 50 orders
	public int CurrentPledges { get; set; } = 0;
	public decimal PledgePrice { get; set; } // Discounted price

	public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
	public DateTime Deadline { get; set; }
}

// 6. Shared Group Cart
public class SharedCart
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid HostUserId { get; set; }
	public string JoinCode { get; set; } = string.Empty; // e.g. "TEAM-99"

	// Who is in the cart?
	public List<Guid> ParticipantUserIds { get; set; } = new();

	public bool IsActive { get; set; } = true;
}