using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models; // <--- Critical for Swashbuckle
using System.Text;
using Voya.API.Hubs;
using Voya.Infrastructure;
using Voya.Infrastructure.Persistence;
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

// Safety check to ensure config exists
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

builder.Services.AddHostedService<AbandonedCartService>();
builder.Services.AddHostedService<StoreSchedulerService>();
builder.Services.AddHostedService<ReportGeneratorService>();
builder.Services.AddHttpClient<ExternalLogisticsAdapter>();
builder.Services.AddScoped<InternalLogisticsAdapter>();
builder.Services.AddSignalR();
var app = builder.Build();

// 5. Configure the HTTP Request Pipeline
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication(); // <--- Must be before Authorization
app.UseAuthorization();

app.MapHub<LiveHub>("/hubs/live");

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
	var services = scope.ServiceProvider;
	try
	{
		var context = services.GetRequiredService<VoyaDbContext>();
		// context.Database.Migrate(); // Optional: ensures migrations are applied
		DbInitializer.Seed(context); // <--- Run the seed
	}
	catch (Exception ex)
	{
		var logger = services.GetRequiredService<ILogger<Program>>();
		logger.LogError(ex, "An error occurred while seeding the database.");
	}
}
app.Run();