using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Voya.Application.Common.Interfaces;
using Voya.Infrastructure.Authentication;
using Voya.Infrastructure.Persistence;

namespace Voya.Infrastructure;

public static class DependencyInjection
{
	public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		// Add DB Context
		services.AddDbContext<VoyaDbContext>(options =>
			options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

		// Add Auth Services
		services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
		services.AddSingleton<IPasswordHasher, PasswordHasher>();

		return services;
	}
}