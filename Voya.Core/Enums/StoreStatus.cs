namespace Voya.Core.Enums;

public enum StoreStatus
{
	Draft = 0,              // User is still filling info, hasn't sent it yet
	PendingReview = 1,      // Submitted, waiting for Admin to look at it
	UnderReview = 2,        // Admin is currently checking (documents, etc.)
	ActionRequired = 3,     // Admin needs more info (e.g., bad ID photo)
	ApprovedForContract = 4,// "Reviewed" - Ready for the physical contract/signing
	Active = 5,             // Contract signed, store is LIVE
	Rejected = 6,           // Application denied
	Suspended = 7,           // Was active, but banned for policy violation
	Pending = 8
}