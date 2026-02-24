using BulkLoad.Application.DTOs;
using BulkLoad.Domain.Entities;
using Shared.Contracts.Enums;
using Shared.Contracts.Messages;

namespace BulkLoad.Application.Interfaces;

/// <summary>
/// Servicio principal de procesamiento de carga masiva
/// </summary>
public interface IBulkLoadService
{
    Task<ProcessingResultDto> ProcessBulkLoadAsync(BulkLoadMessage message);
}

/// <summary>
/// Servicio para leer archivos Excel
/// </summary>
public interface IExcelReaderService
{
    Task<List<ExcelRowDto>> ReadExcelAsync(Stream fileStream);
}

/// <summary>
/// Servicio para descargar archivos de SeaweedFS
/// </summary>
public interface IFileDownloadService
{
    Task<Stream> DownloadFileAsync(string fileId);
}

/// <summary>
/// Repositorio para datos procesados
/// </summary>
public interface IDataProcesadaRepository
{
    Task<bool> ExisteCodigoProductoAsync(string codigoProducto);
    Task<int> InsertAsync(DataProcesada data);
    Task InsertBatchAsync(IEnumerable<DataProcesada> dataList);
}

/// <summary>
/// Repositorio para auditoría de fallidos
/// </summary>
public interface IAuditoriaFallidosRepository
{
    Task InsertAsync(AuditoriaFallidos auditoria);
    Task InsertBatchAsync(IEnumerable<AuditoriaFallidos> auditoriaList);
}

/// <summary>
/// Repositorio para gestión de cargas
/// </summary>
public interface ICargaRepository
{
    Task<CargaArchivo?> GetByIdAsync(int id);
    Task<bool> ExisteCargaActivaParaPeriodoAsync(string periodo, int? excludeId = null);
    Task<bool> ExisteCargaFinalizadaParaPeriodoAsync(string periodo);
    Task UpdateEstadoAsync(int id, EstadoCarga estado, string? mensajeError = null);
    Task UpdateResultadosAsync(int id, int totalRegistros, int procesados, int fallidos);
    Task UpdatePeriodoAsync(int id, string periodo);
}

/// <summary>
/// Publicador de mensajes a cola de notificaciones
/// </summary>
public interface INotificationPublisher
{
    Task PublishNotificationAsync(int idCarga, string usuario, string email, 
        int totalProcesados, int totalFallidos, string nombreArchivo);
}
