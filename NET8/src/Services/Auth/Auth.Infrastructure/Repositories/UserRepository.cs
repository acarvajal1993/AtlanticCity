using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Infrastructure.Settings;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly string _connectionString;

    public UserRepository(IOptions<DatabaseSettings> settings)
    {
        _connectionString = settings.Value.ConnectionString;
    }

    private SqlConnection CreateConnection() => new(_connectionString);

    public async Task<User?> GetByUsernameAsync(string username)
    {
        const string sql = """
            SELECT Id, Username, Email, PasswordHash, Role, IsActive, CreatedAt, LastLoginAt
            FROM Users
            WHERE Username = @Username
            """;

        await using var connection = CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Username = username });
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        const string sql = """
            SELECT Id, Username, Email, PasswordHash, Role, IsActive, CreatedAt, LastLoginAt
            FROM Users
            WHERE Id = @Id
            """;

        await using var connection = CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Id = id });
    }

    public async Task UpdateLastLoginAsync(int userId)
    {
        const string sql = """
            UPDATE Users 
            SET LastLoginAt = @LastLoginAt 
            WHERE Id = @Id
            """;

        await using var connection = CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = userId, LastLoginAt = DateTime.UtcNow });
    }
}
