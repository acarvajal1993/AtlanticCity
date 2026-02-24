using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.Interfaces;
using Notifications.Application.Services;
using Notifications.Application.Settings;
using Notifications.Infrastructure.Messaging;
using Notifications.Infrastructure.Repositories;
using Notifications.Infrastructure.Services;

namespace Notifications.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Settings
        services.Configure<RabbitMQSettings>(configuration.GetSection(RabbitMQSettings.SectionName));
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.Configure<DatabaseSettings>(configuration.GetSection(DatabaseSettings.SectionName));

        // Services
        services.AddScoped<NotificationService>();
        services.AddScoped<IEmailService, EmailService>();

        // Repositories
        services.AddScoped<ICargaRepository, CargaRepository>();

        // Background Consumer
        services.AddHostedService<NotificationConsumer>();

        return services;
    }
}
