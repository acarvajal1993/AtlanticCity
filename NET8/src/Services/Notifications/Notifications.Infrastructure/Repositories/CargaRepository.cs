using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Notifications.Application.Interfaces;
using Notifications.Application.Settings;

namespace Notifications.Infrastructure.Repositories;

public class CargaRepository : ICargaRepository
{
    private readonly string _connectionString;

    public CargaRepository(IOptions<DatabaseSettings> settings)
    {
        _connectionString = settings.Value.ConnectionString;
    }

    private SqlConnection CreateConnection() => new(_connectionString);

    public async Task UpdateEstadoNotificadoAsync(int idCarga)
    {
        const string sql = """
            UPDATE CargaArchivo 
            SET Estado = 'Notificado', FechaFin = @FechaFin
            WHERE Id = @Id
            """;

        await using var connection = CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = idCarga, FechaFin = DateTime.UtcNow });
    }
}
