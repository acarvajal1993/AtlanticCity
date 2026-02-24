using Shared.Contracts.Messages;

namespace Notifications.Application.Interfaces;

/// <summary>
/// Servicio de envío de correos
/// </summary>
public interface IEmailService
{
    Task SendNotificationEmailAsync(NotificationMessage message);
}

/// <summary>
/// Repositorio para actualizar estado de carga
/// </summary>
public interface ICargaRepository
{
    Task UpdateEstadoNotificadoAsync(int idCarga);
}
