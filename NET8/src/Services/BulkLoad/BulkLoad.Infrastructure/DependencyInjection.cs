using BulkLoad.Application.Interfaces;
using BulkLoad.Application.Services;
using BulkLoad.Application.Settings;
using BulkLoad.Infrastructure.Messaging;
using BulkLoad.Infrastructure.Repositories;
using BulkLoad.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Resilience;

namespace BulkLoad.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBulkLoadInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Settings
        services.Configure<RabbitMQSettings>(configuration.GetSection(RabbitMQSettings.SectionName));
        services.Configure<SeaweedFSSettings>(configuration.GetSection(SeaweedFSSettings.SectionName));
        services.Configure<DatabaseSettings>(configuration.GetSection(DatabaseSettings.SectionName));
        services.Configure<ProcessingSettings>(configuration.GetSection(ProcessingSettings.SectionName));

        // Services
        services.AddScoped<IBulkLoadService, BulkLoadService>();
        services.AddScoped<IExcelReaderService, ExcelReaderService>();

        // Repositories
        services.AddScoped<ICargaRepository, CargaRepository>();
        services.AddScoped<IDataProcesadaRepository, DataProcesadaRepository>();
        services.AddScoped<IAuditoriaFallidosRepository, AuditoriaFallidosRepository>();

        // Messaging
        services.AddSingleton<INotificationPublisher, NotificationPublisher>();

        // HTTP Client for SeaweedFS con políticas de resiliencia (Retry + Circuit Breaker)
        services.AddHttpClient<IFileDownloadService, FileDownloadService>(client =>
        {
            client.Timeout = TimeSpan.FromMinutes(5);
        })
        .AddResiliencePolicies(); // Retry (3 intentos) + Circuit Breaker (5 fallos -> 30s abierto)

        // Background Consumer
        services.AddHostedService<BulkLoadConsumer>();

        return services;
    }
}
