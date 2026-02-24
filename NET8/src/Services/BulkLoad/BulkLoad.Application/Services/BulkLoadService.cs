using BulkLoad.Application.DTOs;
using BulkLoad.Application.Interfaces;
using BulkLoad.Application.Settings;
using BulkLoad.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Contracts.Enums;
using Shared.Contracts.Messages;

namespace BulkLoad.Application.Services;

public class BulkLoadService : IBulkLoadService
{
    private readonly ICargaRepository _cargaRepository;
    private readonly IDataProcesadaRepository _dataProcesadaRepository;
    private readonly IAuditoriaFallidosRepository _auditoriaRepository;
    private readonly IExcelReaderService _excelReader;
    private readonly IFileDownloadService _fileDownload;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly ProcessingSettings _settings;
    private readonly ILogger<BulkLoadService> _logger;

    public BulkLoadService(
        ICargaRepository cargaRepository,
        IDataProcesadaRepository dataProcesadaRepository,
        IAuditoriaFallidosRepository auditoriaRepository,
        IExcelReaderService excelReader,
        IFileDownloadService fileDownload,
        INotificationPublisher notificationPublisher,
        IOptions<ProcessingSettings> settings,
        ILogger<BulkLoadService> logger)
    {
        _cargaRepository = cargaRepository;
        _dataProcesadaRepository = dataProcesadaRepository;
        _auditoriaRepository = auditoriaRepository;
        _excelReader = excelReader;
        _fileDownload = fileDownload;
        _notificationPublisher = notificationPublisher;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<ProcessingResultDto> ProcessBulkLoadAsync(BulkLoadMessage message)
    {
        var result = new ProcessingResultDto { IdCarga = message.IdCarga };
        
        _logger.LogInformation("Iniciando procesamiento de carga {IdCarga}", message.IdCarga);

        try
        {
            // Actualizar estado a En Proceso
            await _cargaRepository.UpdateEstadoAsync(message.IdCarga, EstadoCarga.EnProceso);

            // Descargar archivo de SeaweedFS
            _logger.LogInformation("Descargando archivo desde {RutaArchivo}", message.RutaArchivo);
            using var fileStream = await _fileDownload.DownloadFileAsync(message.RutaArchivo);

            // Leer Excel
            _logger.LogInformation("Leyendo archivo Excel");
            var rows = await _excelReader.ReadExcelAsync(fileStream);
            result.TotalRegistros = rows.Count;

            if (rows.Count == 0)
            {
                await FinalizarConError(message.IdCarga, "El archivo no contiene datos válidos", result);
                return result;
            }

            // Obtener el periodo del primer registro válido
            var periodo = rows.FirstOrDefault(r => !string.IsNullOrEmpty(r.Periodo))?.Periodo;
            
            if (string.IsNullOrEmpty(periodo))
            {
                await FinalizarConError(message.IdCarga, "No se encontró el campo Periodo en el archivo", result);
                return result;
            }

            await _cargaRepository.UpdatePeriodoAsync(message.IdCarga, periodo);

            // Validar duplicidad por periodo (excluyendo la carga actual)
            if (await _cargaRepository.ExisteCargaFinalizadaParaPeriodoAsync(periodo))
            {
                await FinalizarConError(message.IdCarga, 
                    $"Ya existe una carga finalizada para el periodo {periodo}", result);
                return result;
            }

            if (await _cargaRepository.ExisteCargaActivaParaPeriodoAsync(periodo, message.IdCarga))
            {
                await FinalizarConError(message.IdCarga, 
                    $"Ya existe una carga en proceso para el periodo {periodo}", result);
                return result;
            }

            // Procesar registros
            await ProcesarRegistrosAsync(message, rows, result, periodo);

            // Actualizar estado a Cargado
            await _cargaRepository.UpdateEstadoAsync(message.IdCarga, EstadoCarga.Cargado);
            await _cargaRepository.UpdateResultadosAsync(
                message.IdCarga, 
                result.TotalRegistros, 
                result.RegistrosProcesados, 
                result.RegistrosFallidos);

            // Actualizar estado a Finalizado
            await _cargaRepository.UpdateEstadoAsync(message.IdCarga, EstadoCarga.Finalizado);

            // Obtener info de carga para notificación
            var carga = await _cargaRepository.GetByIdAsync(message.IdCarga);

            // Publicar notificación
            await _notificationPublisher.PublishNotificationAsync(
                message.IdCarga,
                message.Usuario,
                carga?.Email ?? $"{message.Usuario}@example.com",
                result.RegistrosProcesados,
                result.RegistrosFallidos,
                message.NombreArchivo);

            result.Exitoso = true;
            _logger.LogInformation("Carga {IdCarga} procesada exitosamente. Procesados: {Procesados}, Fallidos: {Fallidos}",
                message.IdCarga, result.RegistrosProcesados, result.RegistrosFallidos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando carga {IdCarga}", message.IdCarga);
            await FinalizarConError(message.IdCarga, ex.Message, result);
        }

        return result;
    }

    private async Task ProcesarRegistrosAsync(BulkLoadMessage message, List<ExcelRowDto> rows, 
        ProcessingResultDto result, string periodo)
    {
        var datosAInsertar = new List<DataProcesada>();
        var fallidos = new List<AuditoriaFallidos>();

        foreach (var row in rows)
        {
            // Saltar filas vacías
            if (string.IsNullOrWhiteSpace(row.CodigoProducto) && 
                string.IsNullOrWhiteSpace(row.Descripcion))
            {
                continue;
            }

            // Validar fila
            if (!row.EsValido)
            {
                fallidos.Add(CrearAuditoriaFallido(message.IdCarga, row, row.MensajeError ?? "Datos inválidos"));
                result.RegistrosFallidos++;
                continue;
            }

            // Validar código producto requerido
            if (string.IsNullOrWhiteSpace(row.CodigoProducto))
            {
                fallidos.Add(CrearAuditoriaFallido(message.IdCarga, row, "Código de producto vacío"));
                result.RegistrosFallidos++;
                continue;
            }

            // Verificar duplicidad de código producto
            if (await _dataProcesadaRepository.ExisteCodigoProductoAsync(row.CodigoProducto))
            {
                fallidos.Add(CrearAuditoriaFallido(message.IdCarga, row, 
                    $"Código de producto {row.CodigoProducto} ya existe"));
                result.RegistrosDuplicados++;
                result.RegistrosFallidos++;
                continue;
            }

            // Aplicar valores por defecto
            var data = new DataProcesada
            {
                IdCarga = message.IdCarga,
                CodigoProducto = row.CodigoProducto,
                Descripcion = string.IsNullOrWhiteSpace(row.Descripcion) ? "Sin descripción" : row.Descripcion,
                Cantidad = row.Cantidad > 0 ? row.Cantidad : _settings.DefaultCantidad,
                PrecioUnitario = row.PrecioUnitario > 0 ? row.PrecioUnitario : _settings.DefaultPrecio,
                Categoria = string.IsNullOrWhiteSpace(row.Categoria) ? _settings.DefaultCategoria : row.Categoria,
                Periodo = string.IsNullOrWhiteSpace(row.Periodo) ? periodo : row.Periodo,
                FechaProcesamiento = DateTime.UtcNow
            };

            datosAInsertar.Add(data);
            result.RegistrosProcesados++;

            // Insertar en lotes
            if (datosAInsertar.Count >= _settings.BatchSize)
            {
                await _dataProcesadaRepository.InsertBatchAsync(datosAInsertar);
                datosAInsertar.Clear();
            }
        }

        // Insertar registros restantes
        if (datosAInsertar.Count > 0)
        {
            await _dataProcesadaRepository.InsertBatchAsync(datosAInsertar);
        }

        // Insertar fallidos
        if (fallidos.Count > 0)
        {
            await _auditoriaRepository.InsertBatchAsync(fallidos);
        }
    }

    private async Task FinalizarConError(int idCarga, string mensaje, ProcessingResultDto result)
    {
        result.Exitoso = false;
        result.MensajeError = mensaje;
        result.Errores.Add(mensaje);
        
        await _cargaRepository.UpdateEstadoAsync(idCarga, EstadoCarga.Rechazado, mensaje);
        
        _logger.LogWarning("Carga {IdCarga} rechazada: {Mensaje}", idCarga, mensaje);
    }

    private static AuditoriaFallidos CrearAuditoriaFallido(int idCarga, ExcelRowDto row, string motivo)
    {
        return new AuditoriaFallidos
        {
            IdCarga = idCarga,
            NumeroFila = row.NumeroFila,
            CodigoProducto = row.CodigoProducto,
            DatosOriginales = $"Codigo:{row.CodigoProducto}|Desc:{row.Descripcion}|Cant:{row.Cantidad}|Precio:{row.PrecioUnitario}",
            MotivoError = motivo,
            FechaRegistro = DateTime.UtcNow
        };
    }
}
