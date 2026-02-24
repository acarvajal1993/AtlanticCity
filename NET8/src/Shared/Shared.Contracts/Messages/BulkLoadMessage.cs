namespace Shared.Contracts.Messages;

/// <summary>
/// Mensaje para la cola de carga masiva
/// </summary>
public record BulkLoadMessage
{
    public int IdCarga { get; init; }
    public string RutaArchivo { get; init; } = string.Empty;
    public string Usuario { get; init; } = string.Empty;
    public string NombreArchivo { get; init; } = string.Empty;
    public DateTime FechaCreacion { get; init; } = DateTime.UtcNow;
}
