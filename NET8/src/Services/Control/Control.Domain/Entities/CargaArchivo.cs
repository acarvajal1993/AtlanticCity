using Shared.Contracts.Enums;

namespace Control.Domain.Entities;

/// <summary>
/// Entidad que representa una carga de archivo
/// </summary>
public class CargaArchivo
{
    public int Id { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    public EstadoCarga Estado { get; set; } = EstadoCarga.Pendiente;
    public DateTime? FechaFin { get; set; }
    public string? RutaArchivo { get; set; }
    public long TamanoArchivo { get; set; }
    public string? MensajeError { get; set; }
    public int TotalRegistros { get; set; }
    public int RegistrosProcesados { get; set; }
    public int RegistrosFallidos { get; set; }
}
