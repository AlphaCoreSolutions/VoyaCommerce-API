using Hangfire; // <--- NEW
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Voya.API.Hubs;
using Voya.Infrastructure;
using Voya.Infrastructure.Persistence;
using Voya.Infrastructure.Services; // <--- For AuctionBackgroundService
using Voya.Infrastructure.Services.Logistics;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSignalR();

// 2. Configure Swagger (Swashbuckle)
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo { Title = "VOYA Commerce API", Version = "v1" });

	// Define the Security Scheme (JWT)
	c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
		Name = "Authorization",
		In = ParameterLocation.Header,
		Type = SecuritySchemeType.Http,
		Scheme = "Bearer"
	});

	// Apply the Security Scheme globally
	c.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme
			{
				Reference = new OpenApiReference
				{
					Type = ReferenceType.SecurityScheme,
					Id = "Bearer"
				}
			},
			new string[] {}
		}
	});
});

// 3. Add Custom Infrastructure Layer
builder.Services.AddInfrastructure(builder.Configuration);

// 4. Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"];

if (string.IsNullOrEmpty(secretKey))
{
	throw new Exception("JWT Secret is missing in appsettings.json");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			ValidIssuer = jwtSettings["Issuer"],
			ValidAudience = jwtSettings["Audience"],
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
		};
	});

// 5. Register Hosted Services & Background Logic
builder.Services.AddHostedService<AbandonedCartService>();
builder.Services.AddHostedService<StoreSchedulerService>();
builder.Services.AddHostedService<ReportGeneratorService>();

// Logistics
builder.Services.AddHttpClient<ExternalLogisticsAdapter>();
builder.Services.AddScoped<InternalLogisticsAdapter>();

// === NEW: Hangfire & Auction Logic ===
// Register the logic service that handles closing auctions
builder.Services.AddScoped<IAuctionBackgroundService, AuctionBackgroundService>();

// Register Hangfire Core
builder.Services.AddHangfire(configuration => configuration
	.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
	.UseSimpleAssemblyNameTypeSerializer()
	.UseRecommendedSerializerSettings()
	.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register the Hangfire Worker Server
builder.Services.AddHangfireServer();

var app = builder.Build();

// 6. Configure the HTTP Request Pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
	c.SwaggerEndpoint("/swagger/v1/swagger.json", "VOYA API V1");
	c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// === NEW: Hangfire Dashboard ===
// Access at: http://localhost:5000/hangfire
app.UseHangfireDashboard("/hangfire");

app.MapHub<LiveHub>("/hubs/live");
app.MapControllers();

// 7. Data Seeding & Job Scheduling
using (var scope = app.Services.CreateScope())
{
	var services = scope.ServiceProvider;
	try
	{
		var context = services.GetRequiredService<VoyaDbContext>();
		// context.Database.Migrate(); // Optional
		DbInitializer.Seed(context);

		// === NEW: Schedule Auction Checks ===
		// Runs every minute to close expired auctions
		var recurringJobManager = services.GetRequiredService<IRecurringJobManager>();

		recurringJobManager.AddOrUpdate<IAuctionBackgroundService>(
			"check-auctions",
			service => service.CheckExpiredAuctions(),
			Cron.Minutely
		);
	}
	catch (Exception ex)
	{
		var logger = services.GetRequiredService<ILogger<Program>>();
		logger.LogError(ex, "An error occurred during initialization/seeding.");
	}
}

app.Run();