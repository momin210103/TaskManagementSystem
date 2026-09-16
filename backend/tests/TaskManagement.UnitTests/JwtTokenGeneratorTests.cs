using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using TaskManagement.Infrastructure.Authentication;

namespace TaskManagement.UnitTests;

public class JwtTokenGeneratorTests
{
    [Fact]
    public void GenerateToken_ShouldCreateValidJwtWithRequiredClaims()
    {
        var options = new JwtOptions
        {
            Secret = "SuperSecretKeyForJwtTesting1234567890!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpirationMinutes = 30
        };
        var generator = new JwtTokenGenerator(Options.Create(options));

        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var role = "Manager";

        var tokenString = generator.GenerateToken(userId, email, role);

        Assert.False(string.IsNullOrWhiteSpace(tokenString));

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(tokenString);

        Assert.Equal("TestIssuer", jwtToken.Issuer);
        Assert.Contains("TestAudience", jwtToken.Audiences);

        var subClaim = jwtToken.Claims.FirstOrDefault(c => string.Equals(c.Type, JwtRegisteredClaimNames.Sub, StringComparison.Ordinal) || string.Equals(c.Type, ClaimTypes.NameIdentifier, StringComparison.Ordinal));
        Assert.NotNull(subClaim);
        Assert.Equal(userId.ToString(), subClaim.Value);

        var emailClaim = jwtToken.Claims.FirstOrDefault(c => string.Equals(c.Type, JwtRegisteredClaimNames.Email, StringComparison.Ordinal) || string.Equals(c.Type, ClaimTypes.Email, StringComparison.Ordinal));
        Assert.NotNull(emailClaim);
        Assert.Equal(email, emailClaim.Value);

        var roleClaim = jwtToken.Claims.FirstOrDefault(c => string.Equals(c.Type, ClaimTypes.Role, StringComparison.Ordinal) || string.Equals(c.Type, "role", StringComparison.Ordinal));
        Assert.NotNull(roleClaim);
        Assert.Equal(role, roleClaim.Value);
    }
}

