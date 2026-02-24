using System.ComponentModel.DataAnnotations;

namespace Auth.Application.DTOs;

public record LoginRequest
{
    [Required(ErrorMessage = "El usuario es requerido")]
    public string Username { get; init; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida")]
    public string Password { get; init; } = string.Empty;
}

public record LoginResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}

public record RefreshTokenRequest
{
    [Required(ErrorMessage = "El refresh token es requerido")]
    public string RefreshToken { get; init; } = string.Empty;
}
