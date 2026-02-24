using Control.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Shared.Contracts.Enums;

namespace Control.Application.Interfaces;

/// <summary>
/// Repositorio para gestionar las cargas de archivos
/// </summary>
public interface ICargaArchivoRepository
{
    Task<CargaArchivo> CreateAsync(CargaArchivo carga);
    Task<CargaArchivo?> GetByIdAsync(int id);
    Task<IEnumerable<CargaArchivo>> GetByUsuarioAsync(string usuario);
    Task<IEnumerable<CargaArchivo>> GetAllAsync(int page = 1, int pageSize = 10);
    Task UpdateEstadoAsync(int id, EstadoCarga estado, string? mensajeError = null);
    Task UpdateRutaArchivoAsync(int id, string rutaArchivo);
}

/// <summary>
/// Servicio para subir archivos a SeaweedFS
/// </summary>
public interface IFileStorageService
{
    Task<string> UploadFileAsync(IFormFile file, string fileName);
    Task<Stream> DownloadFileAsync(string fileId);
    Task DeleteFileAsync(string fileId);
}

/// <summary>
/// Servicio para publicar mensajes en RabbitMQ
/// </summary>
public interface IMessagePublisher
{
    Task PublishBulkLoadMessageAsync(int idCarga, string rutaArchivo, string usuario, string nombreArchivo);
    Task PublishNotificationMessageAsync(int idCarga, string usuario, string email, int totalProcesados, int totalFallidos, string nombreArchivo);
}

/// <summary>
/// Servicio principal de control de cargas
/// </summary>
public interface IControlService
{
    Task<CargaArchivo> IniciarCargaAsync(IFormFile file, string usuario, string email);
    Task<CargaArchivo?> ObtenerCargaAsync(int id);
    Task<IEnumerable<CargaArchivo>> ObtenerHistorialAsync(string usuario);
    Task<IEnumerable<CargaArchivo>> ObtenerTodasLasCargasAsync(int page = 1, int pageSize = 10);
}
