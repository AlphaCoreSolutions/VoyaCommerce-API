using System.Collections.Generic;
using System.Linq; // Required for SequenceEqual
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking; // Required for ValueComparer
using Voya.Core.Entities;

namespace Voya.Infrastructure.Persistence;

public class VoyaDbContext : DbContext
{
	public VoyaDbContext(DbContextOptions<VoyaDbContext> options) : base(options) { }

	public DbSet<User> Users { get; set; }
	public DbSet<Product> Products { get; set; }
	public DbSet<ProductOption> ProductOptions { get; set; }
	public DbSet<Category> Categories { get; set; }
	public DbSet<Order> Orders { get; set; }
	public DbSet<OrderItem> OrderItems { get; set; }
	public DbSet<Address> Addresses { get; set; }
	public DbSet<Cart> Carts { get; set; }
	public DbSet<CartItem> CartItems { get; set; }
	public DbSet<PaymentMethod> PaymentMethods { get; set; }
	public DbSet<Voucher> Vouchers { get; set; }
	public DbSet<UserVoucher> UserVouchers { get; set; }
	public DbSet<GameHistory> GameHistories { get; set; }
	public DbSet<Ticket> Tickets { get; set; }
	public DbSet<Review> Reviews { get; set; }
	public DbSet<WishlistItem> WishlistItems { get; set; }
	public DbSet<Notification> Notifications { get; set; }

	public DbSet<ReverseAuction> ReverseAuctions { get; set; }
	public DbSet<StyleBoard> StyleBoards { get; set; }
	public DbSet<GiftOrder> GiftOrders { get; set; }
	public DbSet<CrowdFundCampaign> CrowdFundCampaigns { get; set; }
	public DbSet<SharedCart> SharedCarts { get; set; }
	public DbSet<Store> Stores { get; set; }
	public DbSet<FlashSale> FlashSales { get; set; }
	public DbSet<ProductBundle> ProductBundles { get; set; }
	public DbSet<StoreStaff> StoreStaff { get; set; }
	public DbSet<LiveEvent> LiveEvents { get; set; }
	public DbSet<ReturnRequest> ReturnRequests { get; set; }
	public DbSet<WalletTransaction> WalletTransactions { get; set; }
	public DbSet<PayoutRequest> PayoutRequests { get; set; }
	public DbSet<StoreAuditLog> StoreAuditLogs { get; set; }

	// New Multi-Warehouse
	public DbSet<Warehouse> Warehouses { get; set; }
	public DbSet<ProductInventory> ProductInventories { get; set; }

	// New Features
	public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
	public DbSet<BlacklistRequest> BlacklistRequests { get; set; }
	public DbSet<ProductTierPrice> ProductTierPrices { get; set; }

	public DbSet<StaffShift> StaffShifts { get; set; }
	public DbSet<CompetitorAlert> CompetitorAlerts { get; set; }
	public DbSet<CustomerTag> CustomerTags { get; set; }
	public DbSet<StorePickupSettings> PickupSettings { get; set; }
	public DbSet<GiftWrapOption> GiftWrapOptions { get; set; }
	public DbSet<NexusRole> NexusRoles { get; set; }
	public DbSet<SystemAuditLog> SystemAuditLogs { get; set; }
	public DbSet<GlobalSetting> GlobalSettings { get; set; }
	public DbSet<FeatureFlag> FeatureFlags { get; set; }
	public DbSet<LocalizationResource> LocalizationResources { get; set; }
	public DbSet<SensitiveAccessLog> SensitiveAccessLogs { get; set; }
	public DbSet<GlobalCoupon> GlobalCoupons { get; set; }
	public DbSet<AppVersionConfig> AppVersionConfigs { get; set; }
	public DbSet<PayrollRecord> PayrollRecords { get; set; }
	public DbSet<RecycleBinItem> RecycleBinItems { get; set; }
	public DbSet<FraudAlert> FraudAlerts { get; set; }
	public DbSet<AbTestExperiment> AbTestExperiments { get; set; }
	public DbSet<AdminTask> AdminTasks { get; set; }
	public DbSet<WebhookSubscription> WebhookSubscriptions { get; set; }
	public DbSet<SeoMetaTag> SeoMetaTags { get; set; }
	public DbSet<NpsScore> NpsScores { get; set; }
	public DbSet<Announcement> Announcements { get; set; }
	public DbSet<LegalDocument> LegalDocuments { get; set; }
	public DbSet<IpBlockRule> IpBlockRules { get; set; }
	public DbSet<ReportSchedule> ReportSchedules { get; set; }
	public DbSet<ExternalApiKey> ExternalApiKeys { get; set; }
	public DbSet<SupportAssignmentRule> SupportAssignmentRules { get; set; }
	public DbSet<Country> Countries { get; set; }
	public DbSet<Currency> Currencies { get; set; }
	public DbSet<ReferralConfig> ReferralConfigs { get; set; }
	public DbSet<GeneratedReport> GeneratedReports { get; set; }
	public DbSet<LogisticsProvider> LogisticsProviders { get; set; }
	public DbSet<Shipment> Shipments { get; set; }
	public DbSet<DriverProfile> DriverProfiles { get; set; }
	public DbSet<DeliveryTask> DeliveryTasks { get; set; }
	public DbSet<CannedResponse> CannedResponses { get; set; }
	public DbSet<AdminInternalNote> AdminInternalNotes { get; set; }
	public DbSet<CashRefundAppointment> CashRefundAppointments { get; set; }
	public DbSet<StoreStrike> StoreStrikes { get; set; }
	public DbSet<FaqItem> FaqItems { get; set; }
	public DbSet<StockSubscription> StockSubscriptions { get; set; }
	public DbSet<SellerVerification> SellerVerifications { get; set; }
	public DbSet<HomepageWidget> HomepageWidgets { get; set; }
	public DbSet<UserBadge> UserBadges { get; set; }
	public DbSet<Campaign> Campaigns { get; set; }
	public DbSet<AffiliateProfile> AffiliateProfiles { get; set; }
	public DbSet<SystemReport> SystemReports { get; set; }
	public DbSet<WikiArticle> WikiArticles { get; set; }
	public DbSet<TicketMessage> TicketMessages { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		// 1. Configure User
		modelBuilder.Entity<User>()
			.HasIndex(u => u.Email)
			.IsUnique();

		// 2. Configure Product (Images & Tags)
		modelBuilder.Entity<Product>()
			.Property(p => p.GalleryImages)
			.HasConversion(
				v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
				v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
			)
			.Metadata.SetValueComparer(new ValueComparer<List<string>>(
				(c1, c2) => c1!.SequenceEqual(c2!),
				c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
				c => c.ToList()));

		// --- NEW: Product Tags ---
		modelBuilder.Entity<Product>()
			.Property(p => p.Tags)
			.HasConversion(
				v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
				v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
			)
			.Metadata.SetValueComparer(new ValueComparer<List<string>>(
				(c1, c2) => c1!.SequenceEqual(c2!),
				c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
				c => c.ToList()));

		// 3. Configure Product -> Options
		modelBuilder.Entity<Product>()
			.HasMany(p => p.Options)
			.WithOne()
			.HasForeignKey(o => o.ProductId)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<ProductOption>()
			.Property(o => o.Values)
			.HasConversion(
				v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
				v => JsonSerializer.Deserialize<List<ProductOptionValue>>(v, (JsonSerializerOptions?)null) ?? new List<ProductOptionValue>()
			)
			.Metadata.SetValueComparer(new ValueComparer<List<ProductOptionValue>>(
				// Deep comparison using JSON serialization to check if values changed
				(c1, c2) => JsonSerializer.Serialize(c1, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(c2, (JsonSerializerOptions?)null),
				c => JsonSerializer.Serialize(c, (JsonSerializerOptions?)null).GetHashCode(),
				c => JsonSerializer.Deserialize<List<ProductOptionValue>>(JsonSerializer.Serialize(c, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null)!));

		// 4. Configure Category (Recursive)
		modelBuilder.Entity<Category>()
			.HasOne(c => c.Parent)
			.WithMany(c => c.SubCategories)
			.HasForeignKey(c => c.ParentId)
			.OnDelete(DeleteBehavior.Restrict);

		// 5. Configure Order
		modelBuilder.Entity<Order>()
			.Property(o => o.Status)
			.HasConversion<string>();

		modelBuilder.Entity<Order>()
			.Property(o => o.PaymentStatus)
			.HasConversion<string>();

		modelBuilder.Entity<Order>()
		.HasMany(o => o.Items)
		.WithOne(i => i.Order)
		.HasForeignKey(i => i.OrderId)
		.OnDelete(DeleteBehavior.Cascade);

		// Configure Indexes
		modelBuilder.Entity<PaymentMethod>().HasIndex(p => p.UserId);
		modelBuilder.Entity<Voucher>().HasIndex(v => v.Code).IsUnique();
		modelBuilder.Entity<GameHistory>().HasIndex(g => g.UserId);
		modelBuilder.Entity<GameHistory>().HasIndex(g => g.PlayedAt);

		// 6. Review Images
		modelBuilder.Entity<Review>()
			.Property(r => r.ImageUrls)
			.HasConversion(
				v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
				v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
			)
			.Metadata.SetValueComparer(new ValueComparer<List<string>>(
				(c1, c2) => c1!.SequenceEqual(c2!),
				c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
				c => c.ToList()));

		// 7. StyleBoard Products
		modelBuilder.Entity<StyleBoard>()
			.Property(s => s.ProductIds)
			.HasConversion(
				v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
				v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>()
			)
			.Metadata.SetValueComparer(new ValueComparer<List<Guid>>(
				(c1, c2) => c1!.SequenceEqual(c2!),
				c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
				c => c.ToList()));

		// 8. Shared Cart Participants
		modelBuilder.Entity<SharedCart>()
			.Property(s => s.ParticipantUserIds)
			.HasConversion(
				v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
				v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>()
			)
			.Metadata.SetValueComparer(new ValueComparer<List<Guid>>(
				(c1, c2) => c1!.SequenceEqual(c2!),
				c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
				c => c.ToList()));

		modelBuilder.Entity<Store>()
	.HasIndex(s => s.OwnerId)
	.IsUnique(); // One store per user (for now)

		modelBuilder.Entity<Store>()
			.Property(s => s.Status)
			.HasConversion<string>(); // Save as string for readability

		modelBuilder.Entity<NexusRole>()
		.Property(e => e.Permissions)
		.HasConversion(
			v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null!), // List -> String
			v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions)null!) ?? new List<string>() // String -> List
		);
	}
}