namespace Shared.Contracts.Enums;

/// <summary>
/// Estados posibles de una carga de archivo
/// </summary>
public enum EstadoCarga
{
    Pendiente = 1,
    EnProceso = 2,
    Cargado = 3,
    Finalizado = 4,
    Notificado = 5,
    Rechazado = 6,
    Error = 7
}
