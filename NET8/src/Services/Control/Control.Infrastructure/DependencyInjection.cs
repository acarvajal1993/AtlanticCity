using Control.Application.Interfaces;
using Control.Application.Services;
using Control.Application.Settings;
using Control.Infrastructure.Messaging;
using Control.Infrastructure.Repositories;
using Control.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Resilience;

namespace Control.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddControlInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Settings
        services.Configure<FileSettings>(configuration.GetSection(FileSettings.SectionName));
        services.Configure<RabbitMQSettings>(configuration.GetSection(RabbitMQSettings.SectionName));
        services.Configure<SeaweedFSSettings>(configuration.GetSection(SeaweedFSSettings.SectionName));
        services.Configure<DatabaseSettings>(configuration.GetSection(DatabaseSettings.SectionName));

        // Services
        services.AddScoped<IControlService, ControlService>();

        // Repositories
        services.AddScoped<ICargaArchivoRepository, CargaArchivoRepository>();

        // Messaging
        services.AddSingleton<IMessagePublisher, RabbitMQPublisher>();

        // HTTP Client for SeaweedFS con políticas de resiliencia (Retry + Circuit Breaker)
        services.AddHttpClient<IFileStorageService, SeaweedFSService>(client =>
        {
            client.Timeout = TimeSpan.FromMinutes(5);
        })
        .AddResiliencePolicies(); // Retry (3 intentos) + Circuit Breaker (5 fallos -> 30s abierto)

        return services;
    }
}
