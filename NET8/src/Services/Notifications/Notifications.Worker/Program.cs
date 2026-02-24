using Notifications.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

// Add Infrastructure services (includes NotificationConsumer as HostedService)
builder.Services.AddNotificationsInfrastructure(builder.Configuration);

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var host = builder.Build();
host.Run();
