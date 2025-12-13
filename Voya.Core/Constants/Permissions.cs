namespace Voya.Core.Constants;

public static class Permissions
{
	// Users
	public const string UsersView = "users.view";
	public const string UsersManage = "users.manage";

	// Stores
	public const string StoresView = "stores.view";
	public const string StoresApprove = "stores.approve";
	public const string StoresManage = "stores.manage";

	// Orders (ADDED THESE)
	public const string OrdersView = "orders.view";
	public const string OrdersManage = "orders.manage";

	// Finances
	public const string FinanceView = "finance.view";
	public const string FinancePayout = "finance.payout";
	public const string FinanceRefund = "finance.refund";

	// Content
	public const string ContentManage = "content.manage";

	// Marketing (Fixes NexusMarketingController)
	public const string MarketingManage = "marketing.manage";

	// System
	public const string SystemConfig = "system.config";
	public const string StaffManage = "staff.manage";
	public const string LogisticsManage = "logistics.manage";
}