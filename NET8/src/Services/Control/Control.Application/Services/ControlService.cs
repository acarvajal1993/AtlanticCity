using Control.Application.Interfaces;
using Control.Application.Settings;
using Control.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Contracts.Enums;

namespace Control.Application.Services;

public class ControlService : IControlService
{
    private readonly ICargaArchivoRepository _cargaRepository;
    private readonly IFileStorageService _fileStorage;
    private readonly IMessagePublisher _messagePublisher;
    private readonly FileSettings _fileSettings;
    private readonly ILogger<ControlService> _logger;

    public ControlService(
        ICargaArchivoRepository cargaRepository,
        IFileStorageService fileStorage,
        IMessagePublisher messagePublisher,
        IOptions<FileSettings> fileSettings,
        ILogger<ControlService> logger)
    {
        _cargaRepository = cargaRepository;
        _fileStorage = fileStorage;
        _messagePublisher = messagePublisher;
        _fileSettings = fileSettings.Value;
        _logger = logger;
    }

    public async Task<CargaArchivo> IniciarCargaAsync(IFormFile file, string usuario, string email)
    {
        _logger.LogInformation("Iniciando carga de archivo {FileName} para usuario {Usuario}", 
            file.FileName, usuario);

        // Validar tamaño del archivo
        if (file.Length > _fileSettings.MaxFileSizeBytes)
        {
            throw new InvalidOperationException(
                $"El archivo excede el tamaño máximo permitido de {_fileSettings.MaxFileSizeBytes / 1024 / 1024}MB");
        }

        // Validar extensión
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_fileSettings.AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException(
                $"Extensión no permitida. Extensiones válidas: {string.Join(", ", _fileSettings.AllowedExtensions)}");
        }

        // Crear registro de carga con estado Pendiente
        var carga = new CargaArchivo
        {
            NombreArchivo = file.FileName,
            Usuario = usuario,
            Email = email,
            FechaRegistro = DateTime.UtcNow,
            Estado = EstadoCarga.Pendiente,
            TamanoArchivo = file.Length
        };

        carga = await _cargaRepository.CreateAsync(carga);
        _logger.LogInformation("Carga registrada con ID {IdCarga}", carga.Id);

        try
        {
            // Subir archivo a SeaweedFS
            var fileName = $"{carga.Id}_{file.FileName}";
            var rutaArchivo = await _fileStorage.UploadFileAsync(file, fileName);
            
            await _cargaRepository.UpdateRutaArchivoAsync(carga.Id, rutaArchivo);
            carga.RutaArchivo = rutaArchivo;
            
            _logger.LogInformation("Archivo subido a SeaweedFS: {RutaArchivo}", rutaArchivo);

            // Publicar mensaje en cola de carga masiva
            await _messagePublisher.PublishBulkLoadMessageAsync(
                carga.Id, 
                rutaArchivo, 
                usuario, 
                file.FileName);

            _logger.LogInformation("Mensaje publicado en cola para carga {IdCarga}", carga.Id);

            return carga;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar carga {IdCarga}", carga.Id);
            await _cargaRepository.UpdateEstadoAsync(carga.Id, EstadoCarga.Error, ex.Message);
            throw;
        }
    }

    public async Task<CargaArchivo?> ObtenerCargaAsync(int id)
    {
        return await _cargaRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<CargaArchivo>> ObtenerHistorialAsync(string usuario)
    {
        return await _cargaRepository.GetByUsuarioAsync(usuario);
    }

    public async Task<IEnumerable<CargaArchivo>> ObtenerTodasLasCargasAsync(int page = 1, int pageSize = 10)
    {
        return await _cargaRepository.GetAllAsync(page, pageSize);
    }
}
