using TaskManagement.Infrastructure.Authentication;

namespace TaskManagement.UnitTests;

public class RefreshTokenServiceTests
{
    [Fact]
    public void GenerateRefreshToken_ShouldProduceCryptographicallyRandomString()
    {
        var service = new RefreshTokenService();

        var token1 = service.GenerateRefreshToken();
        var token2 = service.GenerateRefreshToken();

        Assert.False(string.IsNullOrWhiteSpace(token1));
        Assert.False(string.IsNullOrWhiteSpace(token2));
        Assert.NotEqual(token1, token2);
        Assert.True(token1.Length >= 64);
    }

    [Fact]
    public void HashToken_ShouldProduceConsistentSha256Hex()
    {
        var service = new RefreshTokenService();
        var rawToken = "my-secure-random-token-string";

        var hash1 = service.HashToken(rawToken);
        var hash2 = service.HashToken(rawToken);

        Assert.Equal(hash1, hash2);
        Assert.Equal(64, hash1.Length); // 256 bits = 32 bytes = 64 hex characters
    }

    [Fact]
    public void HashToken_DifferentInputs_ShouldProduceDifferentHashes()
    {
        var service = new RefreshTokenService();

        var hash1 = service.HashToken("token-a");
        var hash2 = service.HashToken("token-b");

        Assert.NotEqual(hash1, hash2);
    }
}

