using Auth.Application.DTOs;
using Auth.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Auth.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        ITokenService tokenService,
        IPasswordHasher passwordHasher,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        _logger.LogInformation("Intento de login para usuario: {Username}", request.Username);

        var user = await _userRepository.GetByUsernameAsync(request.Username);
        
        if (user is null)
        {
            _logger.LogWarning("Usuario no encontrado: {Username}", request.Username);
            return null;
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Usuario inactivo: {Username}", request.Username);
            return null;
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Contraseña incorrecta para usuario: {Username}", request.Username);
            return null;
        }

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();
        
        _tokenService.StoreRefreshToken(user.Id, refreshToken);
        await _userRepository.UpdateLastLoginAsync(user.Id);

        _logger.LogInformation("Login exitoso para usuario: {Username}", request.Username);

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            Username = user.Username,
            Email = user.Email,
            Role = user.Role
        };
    }

    public async Task<LoginResponse?> RefreshTokenAsync(string refreshToken)
    {
        var userId = _tokenService.ValidateRefreshToken(refreshToken);
        
        if (userId is null)
        {
            _logger.LogWarning("Refresh token inválido o expirado");
            return null;
        }

        var user = await _userRepository.GetByIdAsync(userId.Value);
        
        if (user is null || !user.IsActive)
        {
            _logger.LogWarning("Usuario no encontrado o inactivo para refresh token");
            return null;
        }

        _tokenService.RevokeRefreshToken(refreshToken);
        
        var newAccessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        
        _tokenService.StoreRefreshToken(user.Id, newRefreshToken);

        _logger.LogInformation("Token renovado para usuario: {Username}", user.Username);

        return new LoginResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            Username = user.Username,
            Email = user.Email,
            Role = user.Role
        };
    }

    public Task<bool> ValidateTokenAsync(string token)
    {
        // La validación del JWT se hace en el middleware
        return Task.FromResult(true);
    }
}
