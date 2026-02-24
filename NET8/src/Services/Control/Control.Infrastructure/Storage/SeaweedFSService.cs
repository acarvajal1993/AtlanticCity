using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Control.Application.Interfaces;
using Control.Application.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Control.Infrastructure.Storage;

public class SeaweedFSService : IFileStorageService
{
    private readonly HttpClient _httpClient;
    private readonly SeaweedFSSettings _settings;
    private readonly ILogger<SeaweedFSService> _logger;

    public SeaweedFSService(
        HttpClient httpClient, 
        IOptions<SeaweedFSSettings> settings,
        ILogger<SeaweedFSService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> UploadFileAsync(IFormFile file, string fileName)
    {
        // Paso 1: Obtener una ubicación de SeaweedFS Master
        var assignResponse = await _httpClient.GetFromJsonAsync<AssignResponse>(
            $"{_settings.MasterUrl}/dir/assign");

        if (assignResponse is null || string.IsNullOrEmpty(assignResponse.Fid))
        {
            throw new InvalidOperationException("No se pudo obtener ubicación de SeaweedFS");
        }

        _logger.LogInformation("Ubicación asignada: {Fid} en {Url}", 
            assignResponse.Fid, assignResponse.Url);

        // Paso 2: Subir el archivo al Volume Server
        using var content = new MultipartFormDataContent();
        using var stream = file.OpenReadStream();
        using var streamContent = new StreamContent(stream);
        
        content.Add(streamContent, "file", fileName);

        var uploadUrl = $"http://{assignResponse.Url}/{assignResponse.Fid}";
        var uploadResponse = await _httpClient.PostAsync(uploadUrl, content);
        
        if (!uploadResponse.IsSuccessStatusCode)
        {
            var error = await uploadResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Error al subir archivo a SeaweedFS: {error}");
        }

        _logger.LogInformation("Archivo subido exitosamente: {Fid}", assignResponse.Fid);

        // Retornar el identificador del archivo
        return assignResponse.Fid;
    }

    public async Task<Stream> DownloadFileAsync(string fileId)
    {
        // Paso 1: Obtener la ubicación del archivo
        var lookupResponse = await _httpClient.GetFromJsonAsync<LookupResponse>(
            $"{_settings.MasterUrl}/dir/lookup?volumeId={GetVolumeId(fileId)}");

        if (lookupResponse?.Locations is null || lookupResponse.Locations.Count == 0)
        {
            throw new InvalidOperationException($"No se encontró el archivo: {fileId}");
        }

        // Paso 2: Descargar el archivo
        var location = lookupResponse.Locations[0];
        var downloadUrl = $"http://{location.Url}/{fileId}";
        
        var response = await _httpClient.GetAsync(downloadUrl);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStreamAsync();
    }

    public async Task DeleteFileAsync(string fileId)
    {
        var lookupResponse = await _httpClient.GetFromJsonAsync<LookupResponse>(
            $"{_settings.MasterUrl}/dir/lookup?volumeId={GetVolumeId(fileId)}");

        if (lookupResponse?.Locations is null || lookupResponse.Locations.Count == 0)
        {
            _logger.LogWarning("Archivo no encontrado para eliminar: {FileId}", fileId);
            return;
        }

        var location = lookupResponse.Locations[0];
        var deleteUrl = $"http://{location.Url}/{fileId}";
        
        var response = await _httpClient.DeleteAsync(deleteUrl);
        
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Archivo eliminado: {FileId}", fileId);
        }
        else
        {
            _logger.LogWarning("No se pudo eliminar el archivo: {FileId}", fileId);
        }
    }

    private static string GetVolumeId(string fileId)
    {
        var parts = fileId.Split(',');
        return parts.Length > 0 ? parts[0] : fileId;
    }

    // DTOs para las respuestas de SeaweedFS
    private class AssignResponse
    {
        [JsonPropertyName("fid")]
        public string Fid { get; set; } = string.Empty;
        
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
        
        [JsonPropertyName("publicUrl")]
        public string PublicUrl { get; set; } = string.Empty;
        
        [JsonPropertyName("count")]
        public int Count { get; set; }
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
