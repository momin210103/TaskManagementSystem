using System.Security.Cryptography;
using System.Text;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Infrastructure.Authentication;

public class RefreshTokenService : IRefreshTokenService
{
    public string GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }

    public string HashToken(string token)
    {
        ArgumentNullException.ThrowIfNull(token);

        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var hashBytes = SHA256.HashData(tokenBytes);
        return Convert.ToHexStringLower(hashBytes);
    }
}

