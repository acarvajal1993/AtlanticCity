using BulkLoad.Application.Interfaces;
using BulkLoad.Application.Settings;
using BulkLoad.Domain.Entities;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace BulkLoad.Infrastructure.Repositories;

public class DataProcesadaRepository : IDataProcesadaRepository
{
    private readonly string _connectionString;

    public DataProcesadaRepository(IOptions<DatabaseSettings> settings)
    {
        _connectionString = settings.Value.ConnectionString;
    }

    private SqlConnection CreateConnection() => new(_connectionString);

    public async Task<bool> ExisteCodigoProductoAsync(string codigoProducto)
    {
        const string sql = "SELECT COUNT(1) FROM DataProcesada WHERE CodigoProducto = @CodigoProducto";

        await using var connection = CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(sql, new { CodigoProducto = codigoProducto });
        return count > 0;
    }

    public async Task<int> InsertAsync(DataProcesada data)
    {
        const string sql = """
            INSERT INTO DataProcesada (IdCarga, CodigoProducto, Descripcion, Cantidad, 
                                       PrecioUnitario, Categoria, Periodo, FechaProcesamiento)
            OUTPUT INSERTED.Id
            VALUES (@IdCarga, @CodigoProducto, @Descripcion, @Cantidad, 
                    @PrecioUnitario, @Categoria, @Periodo, @FechaProcesamiento)
            """;

        await using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, data);
    }

    public async Task InsertBatchAsync(IEnumerable<DataProcesada> dataList)
    {
        const string sql = """
            INSERT INTO DataProcesada (IdCarga, CodigoProducto, Descripcion, Cantidad, 
                                       PrecioUnitario, Categoria, Periodo, FechaProcesamiento)
            VALUES (@IdCarga, @CodigoProducto, @Descripcion, @Cantidad, 
                    @PrecioUnitario, @Categoria, @Periodo, @FechaProcesamiento)
            """;

        await using var connection = CreateConnection();
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        try
        {
            await connection.ExecuteAsync(sql, dataList, transaction);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}

public class AuditoriaFallidosRepository : IAuditoriaFallidosRepository
{
    private readonly string _connectionString;

    public AuditoriaFallidosRepository(IOptions<DatabaseSettings> settings)
    {
        _connectionString = settings.Value.ConnectionString;
    }

    private SqlConnection CreateConnection() => new(_connectionString);

    public async Task InsertAsync(AuditoriaFallidos auditoria)
    {
        const string sql = """
            INSERT INTO AuditoriaFallidos (IdCarga, NumeroFila, CodigoProducto, 
                                           DatosOriginales, MotivoError, FechaRegistro)
            VALUES (@IdCarga, @NumeroFila, @CodigoProducto, 
                    @DatosOriginales, @MotivoError, @FechaRegistro)
            """;

        await using var connection = CreateConnection();
        await connection.ExecuteAsync(sql, auditoria);
    }

    public async Task InsertBatchAsync(IEnumerable<AuditoriaFallidos> auditoriaList)
    {
        const string sql = """
            INSERT INTO AuditoriaFallidos (IdCarga, NumeroFila, CodigoProducto, 
                                           DatosOriginales, MotivoError, FechaRegistro)
            VALUES (@IdCarga, @NumeroFila, @CodigoProducto, 
                    @DatosOriginales, @MotivoError, @FechaRegistro)
            """;

        await using var connection = CreateConnection();
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        try
        {
            await connection.ExecuteAsync(sql, auditoriaList, transaction);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
