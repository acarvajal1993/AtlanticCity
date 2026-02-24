using Control.Application.Interfaces;
using Control.Application.Settings;
using Control.Domain.Entities;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Shared.Contracts.Enums;

namespace Control.Infrastructure.Repositories;

public class CargaArchivoRepository : ICargaArchivoRepository
{
    private readonly string _connectionString;

    public CargaArchivoRepository(IOptions<DatabaseSettings> settings)
    {
        _connectionString = settings.Value.ConnectionString;
    }

    private SqlConnection CreateConnection() => new(_connectionString);

    public async Task<CargaArchivo> CreateAsync(CargaArchivo carga)
    {
        const string sql = """
            INSERT INTO CargaArchivo (NombreArchivo, Usuario, Email, FechaRegistro, Estado, TamanoArchivo)
            OUTPUT INSERTED.Id
            VALUES (@NombreArchivo, @Usuario, @Email, @FechaRegistro, @Estado, @TamanoArchivo)
            """;

        await using var connection = CreateConnection();
        carga.Id = await connection.ExecuteScalarAsync<int>(sql, new
        {
            carga.NombreArchivo,
            carga.Usuario,
            carga.Email,
            carga.FechaRegistro,
            Estado = carga.Estado.ToString(),
            carga.TamanoArchivo
        });

        return carga;
    }

    public async Task<CargaArchivo?> GetByIdAsync(int id)
    {
        const string sql = """
            SELECT Id, NombreArchivo, Usuario, Email, FechaRegistro, Estado, FechaFin, 
                   RutaArchivo, TamanoArchivo, MensajeError, TotalRegistros, 
                   RegistrosProcesados, RegistrosFallidos
            FROM CargaArchivo
            WHERE Id = @Id
            """;

        await using var connection = CreateConnection();
        var result = await connection.QueryFirstOrDefaultAsync<CargaArchivoDto>(sql, new { Id = id });
        
        return result?.ToEntity();
    }

    public async Task<IEnumerable<CargaArchivo>> GetByUsuarioAsync(string usuario)
    {
        const string sql = """
            SELECT Id, NombreArchivo, Usuario, Email, FechaRegistro, Estado, FechaFin, 
                   RutaArchivo, TamanoArchivo, MensajeError, TotalRegistros, 
                   RegistrosProcesados, RegistrosFallidos
            FROM CargaArchivo
            WHERE Usuario = @Usuario
            ORDER BY FechaRegistro DESC
            """;

        await using var connection = CreateConnection();
        var results = await connection.QueryAsync<CargaArchivoDto>(sql, new { Usuario = usuario });
        
        return results.Select(r => r.ToEntity());
    }

    public async Task<IEnumerable<CargaArchivo>> GetAllAsync(int page = 1, int pageSize = 10)
    {
        const string sql = """
            SELECT Id, NombreArchivo, Usuario, Email, FechaRegistro, Estado, FechaFin, 
                   RutaArchivo, TamanoArchivo, MensajeError, TotalRegistros, 
                   RegistrosProcesados, RegistrosFallidos
            FROM CargaArchivo
            ORDER BY FechaRegistro DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        await using var connection = CreateConnection();
        var results = await connection.QueryAsync<CargaArchivoDto>(sql, new 
        { 
            PageSize = pageSize, 
            Offset = (page - 1) * pageSize 
        });

        return results.Select(r => r.ToEntity());
    }

    public async Task UpdateEstadoAsync(int id, EstadoCarga estado, string? mensajeError = null)
    {
        const string sql = """
            UPDATE CargaArchivo 
            SET Estado = @Estado, 
                MensajeError = @MensajeError,
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

    public async Task UpdateRutaArchivoAsync(int id, string rutaArchivo)
    {
        const string sql = """
            UPDATE CargaArchivo 
            SET RutaArchivo = @RutaArchivo
            WHERE Id = @Id
            """;

        await using var connection = CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id, RutaArchivo = rutaArchivo });
    }

    // DTO interno para mapear desde la base de datos
    private class CargaArchivoDto
    {
        public int Id { get; set; }
        public string NombreArchivo { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime? FechaFin { get; set; }
        public string? RutaArchivo { get; set; }
        public long TamanoArchivo { get; set; }
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
            FechaRegistro = FechaRegistro,
            Estado = Enum.Parse<EstadoCarga>(Estado),
            FechaFin = FechaFin,
            RutaArchivo = RutaArchivo,
            TamanoArchivo = TamanoArchivo,
            MensajeError = MensajeError,
            TotalRegistros = TotalRegistros,
            RegistrosProcesados = RegistrosProcesados,
            RegistrosFallidos = RegistrosFallidos
        };
    }
}
