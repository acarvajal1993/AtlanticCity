using Auth.Application.Interfaces;
using Auth.Application.Services;
using Auth.Infrastructure.Repositories;
using Auth.Infrastructure.Services;
using Auth.Infrastructure.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Settings
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<DatabaseSettings>(configuration.GetSection(DatabaseSettings.SectionName));

        // Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
