using System.Text;
using System.Text.Json;
using BulkLoad.Application.Interfaces;
using BulkLoad.Application.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.Messages;

namespace BulkLoad.Infrastructure.Messaging;

public class BulkLoadConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<BulkLoadConsumer> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public BulkLoadConsumer(
        IServiceProvider serviceProvider,
        IOptions<RabbitMQSettings> settings,
        ILogger<BulkLoadConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BulkLoad Consumer iniciando...");

        await InitializeRabbitMQAsync();

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var messageJson = Encoding.UTF8.GetString(body);

            _logger.LogInformation("Mensaje recibido: {Message}", messageJson);

            try
            {
                var message = JsonSerializer.Deserialize<BulkLoadMessage>(messageJson);

                if (message is not null)
                {
                    // Crear scope para resolver servicios scoped
                    using var scope = _serviceProvider.CreateScope();
                    var bulkLoadService = scope.ServiceProvider.GetRequiredService<IBulkLoadService>();

                    var result = await bulkLoadService.ProcessBulkLoadAsync(message);

                    if (result.Exitoso)
                    {
                        _logger.LogInformation("Carga {IdCarga} procesada exitosamente", message.IdCarga);
                    }
                    else
                    {
                        _logger.LogWarning("Carga {IdCarga} procesada con errores: {Error}", 
                            message.IdCarga, result.MensajeError);
                    }
                }

                // Acknowledge del mensaje
                await _channel!.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando mensaje");
                
                // Requeue del mensaje en caso de error
                await _channel!.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        };

        await _channel!.BasicConsumeAsync(
            queue: _settings.BulkLoadQueue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("BulkLoad Consumer escuchando en cola: {Queue}", _settings.BulkLoadQueue);

        // Mantener el servicio corriendo
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
                    queue: _settings.BulkLoadQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                // Configurar prefetch para procesar un mensaje a la vez
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
        _logger.LogInformation("BulkLoad Consumer deteniendo...");
        
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
