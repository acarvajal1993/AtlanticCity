using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BulkLoad.Application.Interfaces;
using BulkLoad.Application.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BulkLoad.Infrastructure.Services;

public class FileDownloadService : IFileDownloadService
{
    private readonly HttpClient _httpClient;
    private readonly SeaweedFSSettings _settings;
    private readonly ILogger<FileDownloadService> _logger;

    public FileDownloadService(
        HttpClient httpClient,
        IOptions<SeaweedFSSettings> settings,
        ILogger<FileDownloadService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<Stream> DownloadFileAsync(string fileId)
    {
        _logger.LogInformation("Descargando archivo {FileId} desde SeaweedFS", fileId);

        // Obtener ubicación del archivo
        var volumeId = GetVolumeId(fileId);
        var lookupResponse = await _httpClient.GetFromJsonAsync<LookupResponse>(
            $"{_settings.MasterUrl}/dir/lookup?volumeId={volumeId}");

        if (lookupResponse?.Locations is null || lookupResponse.Locations.Count == 0)
        {
            throw new InvalidOperationException($"No se encontró el archivo: {fileId}");
        }

        // Descargar archivo
        var location = lookupResponse.Locations[0];
        var downloadUrl = $"http://{location.Url}/{fileId}";

        _logger.LogInformation("Descargando desde {Url}", downloadUrl);

        var response = await _httpClient.GetAsync(downloadUrl);
        response.EnsureSuccessStatusCode();

        // Crear MemoryStream para poder cerrar la conexión HTTP
        var memoryStream = new MemoryStream();
        await response.Content.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        _logger.LogInformation("Archivo descargado exitosamente: {Size} bytes", memoryStream.Length);

        return memoryStream;
    }

    private static string GetVolumeId(string fileId)
    {
        var parts = fileId.Split(',');
        return parts.Length > 0 ? parts[0] : fileId;
    }

    private class LookupResponse
    {
        [JsonPropertyName("volumeOrFileId")]
        public string VolumeOrFileId { get; set; } = string.Empty;

        [JsonPropertyName("locations")]
        public List<LocationInfo> Locations { get; set; } = [];
    }

    private class LocationInfo
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("publicUrl")]
        public string PublicUrl { get; set; } = string.Empty;
    }
}
