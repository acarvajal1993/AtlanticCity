namespace Shared.Contracts.Messages;

/// <summary>
/// Mensaje para la cola de notificaciones
/// </summary>
public record NotificationMessage
{
    public int IdCarga { get; init; }
    public string Usuario { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public DateTime FechaFin { get; init; } = DateTime.UtcNow;
    public int TotalRegistrosProcesados { get; init; }
    public int TotalRegistrosFallidos { get; init; }
    public string NombreArchivo { get; init; } = string.Empty;
}
