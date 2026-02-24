using System.Security.Cryptography;
using System.Text;
using Auth.Application.Interfaces;

namespace Auth.Infrastructure.Services;

public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    public bool Verify(string password, string hashedPassword)
    {
        var hashOfInput = Hash(password);
        return hashOfInput == hashedPassword;
    }
}
