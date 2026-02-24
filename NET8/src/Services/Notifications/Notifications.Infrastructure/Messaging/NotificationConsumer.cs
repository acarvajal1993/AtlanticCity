using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notifications.Application.Services;
using Notifications.Application.Settings;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.Messages;

namespace Notifications.Infrastructure.Messaging;

public class NotificationConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<NotificationConsumer> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public NotificationConsumer(
        IServiceProvider serviceProvider,
        IOptions<RabbitMQSettings> settings,
        ILogger<NotificationConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification Consumer iniciando...");

        await InitializeRabbitMQAsync();

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var messageJson = Encoding.UTF8.GetString(body);

            _logger.LogInformation("Mensaje de notificación recibido: {Message}", messageJson);

            try
            {
                var message = JsonSerializer.Deserialize<NotificationMessage>(messageJson);

                if (message is not null)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();

                    await notificationService.ProcessNotificationAsync(message);

                    _logger.LogInformation("Notificación procesada para carga {IdCarga}", message.IdCarga);
                }

                await _channel!.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando mensaje de notificación");
                await _channel!.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        };

        await _channel!.BasicConsumeAsync(
            queue: _settings.NotificationQueue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("Notification Consumer escuchando en cola: {Queue}", _settings.NotificationQueue);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task InitializeRabbitMQAsync()
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password,
            VirtualHost = _settings.VirtualHost
        };

        var retryCount = 0;
        const int maxRetries = 10;

        while (retryCount < maxRetries)
        {
            try
            {
                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

                await _channel.QueueDeclareAsync(
                    queue: _settings.NotificationQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                await _channel.BasicQosAsync(0, 1, false);

                _logger.LogInformation("Conexión a RabbitMQ establecida: {Host}:{Port}",
                    _settings.HostName, _settings.Port);
                return;
            }
            catch (Exception ex)
            {
                retryCount++;
                _logger.LogWarning(ex, "Error conectando a RabbitMQ. Intento {Retry}/{MaxRetries}",
                    retryCount, maxRetries);
                await Task.Delay(5000);
            }
        }

        throw new InvalidOperationException("No se pudo conectar a RabbitMQ después de varios intentos");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Notification Consumer deteniendo...");

        if (_channel is not null)
        {
            await _channel.CloseAsync();
            _channel.Dispose();
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }

        await base.StopAsync(cancellationToken);
    }
}
