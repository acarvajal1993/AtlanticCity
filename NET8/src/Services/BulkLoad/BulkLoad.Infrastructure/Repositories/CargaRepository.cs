using BulkLoad.Application.Interfaces;
using BulkLoad.Application.Settings;
using BulkLoad.Domain.Entities;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Shared.Contracts.Enums;

namespace BulkLoad.Infrastructure.Repositories;

public class CargaRepository : ICargaRepository
{
    private readonly string _connectionString;

    public CargaRepository(IOptions<DatabaseSettings> settings)
    {
        _connectionString = settings.Value.ConnectionString;
    }

    private SqlConnection CreateConnection() => new(_connectionString);

    public async Task<CargaArchivo?> GetByIdAsync(int id)
    {
        const string sql = """
            SELECT Id, NombreArchivo, Usuario, Email, Periodo, FechaRegistro, Estado, 
                   FechaFin, RutaArchivo, MensajeError, TotalRegistros, 
                   RegistrosProcesados, RegistrosFallidos
            FROM CargaArchivo WHERE Id = @Id
            """;

        await using var connection = CreateConnection();
        var result = await connection.QueryFirstOrDefaultAsync<CargaArchivoDto>(sql, new { Id = id });
        return result?.ToEntity();
    }

    public async Task<bool> ExisteCargaActivaParaPeriodoAsync(string periodo, int? excludeId = null)
    {
        const string sql = """
            SELECT COUNT(1) FROM CargaArchivo 
            WHERE Periodo = @Periodo 
            AND Estado IN ('Pendiente', 'EnProceso')
            AND (@ExcludeId IS NULL OR Id != @ExcludeId)
            """;

        await using var connection = CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Periodo = periodo, ExcludeId = excludeId });
        return count > 0;
    }

    public async Task<bool> ExisteCargaFinalizadaParaPeriodoAsync(string periodo)
    {
        const string sql = """
            SELECT COUNT(1) FROM CargaArchivo 
            WHERE Periodo = @Periodo AND Estado IN ('Cargado', 'Finalizado', 'Notificado')
            """;

        await using var connection = CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Periodo = periodo });
        return count > 0;
    }

    public async Task UpdateEstadoAsync(int id, EstadoCarga estado, string? mensajeError = null)
    {
        const string sql = """
            UPDATE CargaArchivo 
            SET Estado = @Estado, 
                MensajeError = COALESCE(@MensajeError, MensajeError),
                FechaFin = CASE WHEN @Estado IN ('Finalizado', 'Notificado', 'Error', 'Rechazado') 
                           THEN @FechaFin ELSE FechaFin END
            WHERE Id = @Id
            """;

        await using var connection = CreateConnection();
        await connection.ExecuteAsync(sql, new 
        { 
            Id = id, 
            Estado = estado.ToString(),
            MensajeError = mensajeError,
            FechaFin = DateTime.UtcNow
        });
    }

    public async Task UpdateResultadosAsync(int id, int totalRegistros, int procesados, int fallidos)
    {
        const string sql = """
            UPDATE CargaArchivo 
            SET TotalRegistros = @TotalRegistros,
                RegistrosProcesados = @RegistrosProcesados,
                RegistrosFallidos = @RegistrosFallidos
            WHERE Id = @Id
            """;

        await using var connection = CreateConnection();
        await connection.ExecuteAsync(sql, new 
        { 
            Id = id, 
            TotalRegistros = totalRegistros,
            RegistrosProcesados = procesados,
            RegistrosFallidos = fallidos
        });
    }

    public async Task UpdatePeriodoAsync(int id, string periodo)
    {
        const string sql = "UPDATE CargaArchivo SET Periodo = @Periodo WHERE Id = @Id";

        await using var connection = CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id, Periodo = periodo });
    }

    private class CargaArchivoDto
    {
        public int Id { get; set; }
        public string NombreArchivo { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Periodo { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime? FechaFin { get; set; }
        public string? RutaArchivo { get; set; }
        public string? MensajeError { get; set; }
        public int TotalRegistros { get; set; }
        public int RegistrosProcesados { get; set; }
        public int RegistrosFallidos { get; set; }

        public CargaArchivo ToEntity() => new()
        {
            Id = Id,
            NombreArchivo = NombreArchivo,
            Usuario = Usuario,
            Email = Email,
            Periodo = Periodo,
            FechaRegistro = FechaRegistro,
            Estado = Enum.Parse<EstadoCarga>(Estado),
            FechaFin = FechaFin,
            RutaArchivo = RutaArchivo,
            MensajeError = MensajeError,
            TotalRegistros = TotalRegistros,
            RegistrosProcesados = RegistrosProcesados,
            RegistrosFallidos = RegistrosFallidos
        };
    }
}
