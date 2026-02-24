using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notifications.Application.Interfaces;
using Notifications.Application.Settings;
using Shared.Contracts.Messages;

namespace Notifications.Application.Services;

public class NotificationService
{
    private readonly IEmailService _emailService;
    private readonly ICargaRepository _cargaRepository;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IEmailService emailService,
        ICargaRepository cargaRepository,
        ILogger<NotificationService> logger)
    {
        _emailService = emailService;
        _cargaRepository = cargaRepository;
        _logger = logger;
    }

    public async Task ProcessNotificationAsync(NotificationMessage message)
    {
        _logger.LogInformation("Procesando notificación para carga {IdCarga}", message.IdCarga);

        try
        {
            // Enviar correo
            await _emailService.SendNotificationEmailAsync(message);
            _logger.LogInformation("Correo enviado a {Email}", message.Email);

            // Actualizar estado a Notificado
            await _cargaRepository.UpdateEstadoNotificadoAsync(message.IdCarga);
            _logger.LogInformation("Estado actualizado a Notificado para carga {IdCarga}", message.IdCarga);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando notificación para carga {IdCarga}", message.IdCarga);
            throw;
        }
    }
}
