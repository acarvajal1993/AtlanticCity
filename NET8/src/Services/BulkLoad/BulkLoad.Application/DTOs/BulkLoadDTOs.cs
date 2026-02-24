namespace BulkLoad.Application.DTOs;

/// <summary>
/// Representa una fila del archivo Excel
/// </summary>
public class ExcelRowDto
{
    public int NumeroFila { get; set; }
    public string CodigoProducto { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public string? Categoria { get; set; }
    public string Periodo { get; set; } = string.Empty;
    public bool EsValido { get; set; } = true;
    public string? MensajeError { get; set; }
}

/// <summary>
/// Resultado del procesamiento del archivo
/// </summary>
public class ProcessingResultDto
{
    public int IdCarga { get; set; }
    public int TotalRegistros { get; set; }
    public int RegistrosProcesados { get; set; }
    public int RegistrosFallidos { get; set; }
    public int RegistrosDuplicados { get; set; }
    public bool Exitoso { get; set; }
    public string? MensajeError { get; set; }
    public List<string> Errores { get; set; } = [];
}
