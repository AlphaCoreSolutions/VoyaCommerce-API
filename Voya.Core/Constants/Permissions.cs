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

	// Orders
	public const string OrdersView = "orders.view";
	public const string OrdersManage = "orders.manage";

	// Finances
	public const string FinanceView = "finance.view";
	public const string FinancePayout = "finance.payout";
	public const string FinanceRefund = "finance.refund";

	// === ADDED THESE ===
	// Campaigns (Launchpad)
	public const string CampaignsView = "campaigns.view";

	// Settings (Global Kill Switch)
	public const string SettingsView = "settings.view";
	public const string SettingsManage = "settings.manage";

	// Marketing
	public const string MarketingManage = "marketing.manage";

	// Returns Management
	public const string ReturnsView = "returns.view";
	public const string ReturnsManage = "returns.manage";

	// Taxes Management
	public const string TaxView = "tax.view";
	public const string TaxManage = "tax.manage";

	// CMS Management
	public const string CmsView = "cms.view";
	public const string CmsManage = "cms.manage";

	// Assets Management
	public const string AssetsView = "assets.view";
	public const string AssetsManage = "assets.manage";

	// Global
	public const string GlobalView = "global.view";

	// AI Management
	public const string AiView = "ai.view";
	public const string AiManage = "ai.manage";

	// Security (Nexus)
	public const string SecurityImpersonate = "security.impersonate";
	public const string SecurityLogsView = "security.logs.view";
	public const string SecurityFirewallManage = "security.firewall.manage";
	public const string SecurityApiKeysManage = "security.apikeys.manage";

	// System
	public const string SystemConfig = "system.config";
	public const string StaffManage = "staff.manage";
	public const string LogisticsManage = "logistics.manage";
	public const string ContentManage = "content.manage";
}