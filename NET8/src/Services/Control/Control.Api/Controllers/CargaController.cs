using Control.Application.Interfaces;
using Control.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Control.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CargaController : ControllerBase
{
    private readonly IControlService _controlService;
    private readonly ILogger<CargaController> _logger;

    public CargaController(IControlService controlService, ILogger<CargaController> logger)
    {
        _controlService = controlService;
        _logger = logger;
    }

    /// <summary>
    /// Subir un archivo Excel para procesamiento masivo
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50MB max
    [ProducesResponseType(typeof(CargaArchivo), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No se proporcionó ningún archivo" });

        var usuario = User.Identity?.Name ?? "unknown";
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value 
                    ?? User.FindFirst("email")?.Value 
                    ?? $"{usuario}@example.com";

        try
        {
            var carga = await _controlService.IniciarCargaAsync(file, usuario, email);
            
            return CreatedAtAction(
                nameof(GetById), 
                new { id = carga.Id }, 
                new 
                {
                    carga.Id,
                    carga.NombreArchivo,
                    carga.Estado,
                    carga.FechaRegistro,
                    message = "Archivo recibido y en proceso de carga"
                });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error de validación al subir archivo");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error interno al subir archivo");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener información de una carga por ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CargaArchivo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var carga = await _controlService.ObtenerCargaAsync(id);
        
        if (carga is null)
            return NotFound(new { message = $"Carga con ID {id} no encontrada" });

        return Ok(carga);
    }

    /// <summary>
    /// Obtener historial de cargas del usuario actual
    /// </summary>
    [HttpGet("historial")]
    [ProducesResponseType(typeof(IEnumerable<CargaArchivo>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistorial()
    {
        var usuario = User.Identity?.Name ?? "unknown";
        var historial = await _controlService.ObtenerHistorialAsync(usuario);
        
        return Ok(historial);
    }

    /// <summary>
    /// Obtener todas las cargas (admin)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IEnumerable<CargaArchivo>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var cargas = await _controlService.ObtenerTodasLasCargasAsync(page, pageSize);
        return Ok(cargas);
    }
}
