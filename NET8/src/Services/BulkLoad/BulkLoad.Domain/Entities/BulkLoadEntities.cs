using Shared.Contracts.Enums;

namespace BulkLoad.Domain.Entities;

/// <summary>
/// Registro de carga de archivo (mirror de Control)
/// </summary>
public class CargaArchivo
{
    public int Id { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Periodo { get; set; }
    public DateTime FechaRegistro { get; set; }
    public EstadoCarga Estado { get; set; }
    public DateTime? FechaFin { get; set; }
    public string? RutaArchivo { get; set; }
    public string? MensajeError { get; set; }
    public int TotalRegistros { get; set; }
    public int RegistrosProcesados { get; set; }
    public int RegistrosFallidos { get; set; }
}

/// <summary>
/// Datos procesados del Excel
/// </summary>
public class DataProcesada
{
    public int Id { get; set; }
    public int IdCarga { get; set; }
    public string CodigoProducto { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public string? Categoria { get; set; }
    public string Periodo { get; set; } = string.Empty;
    public DateTime FechaProcesamiento { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Registro de auditoría para filas fallidas
/// </summary>
public class AuditoriaFallidos
{
    public int Id { get; set; }
    public int IdCarga { get; set; }
    public int NumeroFila { get; set; }
    public string? CodigoProducto { get; set; }
    public string DatosOriginales { get; set; } = string.Empty;
    public string MotivoError { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
}
