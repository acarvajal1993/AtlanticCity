using BulkLoad.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

// Add Infrastructure services (includes BulkLoadConsumer as HostedService)
builder.Services.AddBulkLoadInfrastructure(builder.Configuration);

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var host = builder.Build();
host.Run();
