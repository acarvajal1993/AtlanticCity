using Auth.Application.DTOs;
using Auth.Domain.Entities;

namespace Auth.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<LoginResponse?> RefreshTokenAsync(string refreshToken);
    Task<bool> ValidateTokenAsync(string token);
}

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByIdAsync(int id);
    Task UpdateLastLoginAsync(int userId);
}

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    int? ValidateRefreshToken(string refreshToken);
    void StoreRefreshToken(int userId, string refreshToken);
    void RevokeRefreshToken(string refreshToken);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
