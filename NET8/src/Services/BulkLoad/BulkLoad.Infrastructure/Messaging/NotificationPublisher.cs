using System.Text;
using System.Text.Json;
using BulkLoad.Application.Interfaces;
using BulkLoad.Application.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Shared.Contracts.Messages;

namespace BulkLoad.Infrastructure.Messaging;

public class NotificationPublisher : INotificationPublisher, IDisposable
{
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<NotificationPublisher> _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    private bool _disposed;

    public NotificationPublisher(IOptions<RabbitMQSettings> settings, ILogger<NotificationPublisher> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    private async Task EnsureConnectionAsync()
    {
        if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
            return;

        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password,
            VirtualHost = _settings.VirtualHost
        };

        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        await _channel.QueueDeclareAsync(
            queue: _settings.NotificationQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        _logger.LogInformation("Conexión a RabbitMQ establecida para notificaciones");
    }

    public async Task PublishNotificationAsync(int idCarga, string usuario, string email,
        int totalProcesados, int totalFallidos, string nombreArchivo)
    {
        await EnsureConnectionAsync();

        var message = new NotificationMessage
        {
            IdCarga = idCarga,
            Usuario = usuario,
            Email = email,
            FechaFin = DateTime.UtcNow,
            TotalRegistrosProcesados = totalProcesados,
            TotalRegistrosFallidos = totalFallidos,
            NombreArchivo = nombreArchivo
        };

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json"
        };

        await _channel!.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: _settings.NotificationQueue,
            mandatory: true,
            basicProperties: properties,
            body: body);

        _logger.LogInformation("Notificación publicada para carga {IdCarga}", idCarga);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _channel?.Dispose();
        _connection?.Dispose();
        _disposed = true;
    }
}
