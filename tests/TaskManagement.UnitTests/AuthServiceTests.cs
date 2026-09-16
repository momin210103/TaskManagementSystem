using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TaskManagement.Application.DTOs.Auth;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Authentication;
using TaskManagement.Infrastructure.Identity;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.UnitTests;

public sealed class AuthServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IAuthService _authService;
    private readonly IRefreshTokenService _refreshTokenService;

    public AuthServiceTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var dbName = Guid.NewGuid().ToString();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        var jwtOptions = new JwtOptions
        {
            Secret = "TestSecretKeyForTestingTaskManagementSystem2026Net10!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        };
        services.AddSingleton(Options.Create(jwtOptions));

        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IAuthService, AuthService>();

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        _userManager = _serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        _roleManager = _serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        _refreshTokenService = _serviceProvider.GetRequiredService<IRefreshTokenService>();
        _authService = _serviceProvider.GetRequiredService<IAuthService>();

        // Ensure roles exist
        IdentityRoleSeeder.SeedRolesAsync(_roleManager).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task RegisterAsync_Successful_ShouldCreateUserWithDefaultUserRoleAndTokens()
    {
        var request = new RegisterRequest
        {
            Name = "John Doe",
            Email = "john.doe@example.com",
            Password = "Password123"
        };

        var response = await _authService.RegisterAsync(request);

        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));
        Assert.Equal("John Doe", response.User.Name);
        Assert.Equal("john.doe@example.com", response.User.Email);
        Assert.Equal(nameof(UserRole.User), response.User.Role);

        // Verify password hash in Identity
        var createdUser = await _userManager.FindByEmailAsync("john.doe@example.com");
        Assert.NotNull(createdUser);
        Assert.NotNull(createdUser.PasswordHash);
        Assert.NotEqual("Password123", createdUser.PasswordHash);

        // Verify token saved in DB is hashed, not plaintext
        var hashedToken = _refreshTokenService.HashToken(response.RefreshToken);
        var storedRefreshToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(r => r.UserId == createdUser.Id);
        Assert.NotNull(storedRefreshToken);
        Assert.Equal(hashedToken, storedRefreshToken.TokenHash);
        Assert.NotEqual(response.RefreshToken, storedRefreshToken.TokenHash);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ShouldThrowDuplicateEmailException()
    {
        var request = new RegisterRequest
        {
            Name = "Jane Doe",
            Email = "jane.doe@example.com",
            Password = "Password123"
        };

        await _authService.RegisterAsync(request);

        var duplicateRequest = new RegisterRequest
        {
            Name = "Jane Another",
            Email = "jane.doe@example.com",
            Password = "Password456"
        };

        await Assert.ThrowsAsync<DuplicateEmailException>(() => _authService.RegisterAsync(duplicateRequest));
    }

    [Fact]
    public async Task RegisterAsync_ShouldAssignUserRoleAndNeverAdmin()
    {
        var request = new RegisterRequest
        {
            Name = "Normal User",
            Email = "normal.user@example.com",
            Password = "Password123"
        };

        var response = await _authService.RegisterAsync(request);
        Assert.Equal(nameof(UserRole.User), response.User.Role);

        var user = await _userManager.FindByEmailAsync("normal.user@example.com");
        Assert.NotNull(user);
        var roles = await _userManager.GetRolesAsync(user);
        Assert.Contains(nameof(UserRole.User), roles);
        Assert.DoesNotContain(nameof(UserRole.Admin), roles);
        Assert.DoesNotContain(nameof(UserRole.Manager), roles);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ShouldReturnAuthResponse()
    {
        var registerRequest = new RegisterRequest
        {
            Name = "Login User",
            Email = "login.user@example.com",
            Password = "Password123"
        };
        await _authService.RegisterAsync(registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = "login.user@example.com",
            Password = "Password123"
        };

        var response = await _authService.LoginAsync(loginRequest);

        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));
        Assert.Equal("login.user@example.com", response.User.Email);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ShouldThrowAuthException()
    {
        var registerRequest = new RegisterRequest
        {
            Name = "Login User2",
            Email = "login.user2@example.com",
            Password = "Password123"
        };
        await _authService.RegisterAsync(registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = "login.user2@example.com",
            Password = "WrongPassword1"
        };

        await Assert.ThrowsAsync<AuthException>(() => _authService.LoginAsync(loginRequest));
    }

    [Fact]
    public async Task LoginAsync_NonExistentEmail_ShouldThrowAuthException()
    {
        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "Password123"
        };

        await Assert.ThrowsAsync<AuthException>(() => _authService.LoginAsync(loginRequest));
    }

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_ShouldRotateRefreshTokenAndReturnNewAccessToken()
    {
        var registerRequest = new RegisterRequest
        {
            Name = "Refresh User",
            Email = "refresh.user@example.com",
            Password = "Password123"
        };
        var registerResponse = await _authService.RegisterAsync(registerRequest);
        var initialRefreshToken = registerResponse.RefreshToken;

        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = initialRefreshToken
        };

        var refreshResponse = await _authService.RefreshTokenAsync(refreshRequest);

        Assert.NotNull(refreshResponse);
        Assert.False(string.IsNullOrWhiteSpace(refreshResponse.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(refreshResponse.RefreshToken));
        Assert.NotEqual(initialRefreshToken, refreshResponse.RefreshToken);

        // Verify old token is revoked
        var oldTokenHash = _refreshTokenService.HashToken(initialRefreshToken);
        var oldToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == oldTokenHash);
        Assert.NotNull(oldToken);
        Assert.True(oldToken.IsRevoked);
        Assert.NotNull(oldToken.ReplacedByTokenId);

        // Verify new token is active in DB
        var newTokenHash = _refreshTokenService.HashToken(refreshResponse.RefreshToken);
        var newToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == newTokenHash);
        Assert.NotNull(newToken);
        Assert.True(newToken.IsActive);
        Assert.Equal(newToken.Id, oldToken.ReplacedByTokenId);
    }

    [Fact]
    public async Task RefreshTokenAsync_RevokedToken_ShouldThrowAuthException()
    {
        var registerRequest = new RegisterRequest
        {
            Name = "Revoke User",
            Email = "revoke.user@example.com",
            Password = "Password123"
        };
        var registerResponse = await _authService.RegisterAsync(registerRequest);

        // Rotate once
        await _authService.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = registerResponse.RefreshToken });

        // Attempt to use revoked initial token again
        await Assert.ThrowsAsync<AuthException>(() =>
            _authService.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = registerResponse.RefreshToken }));
    }

    [Fact]
    public async Task RefreshTokenAsync_ExpiredToken_ShouldThrowAuthException()
    {
        var registerRequest = new RegisterRequest
        {
            Name = "Expire User",
            Email = "expire.user@example.com",
            Password = "Password123"
        };
        var registerResponse = await _authService.RegisterAsync(registerRequest);

        // Manually expire the token in DB
        var tokenHash = _refreshTokenService.HashToken(registerResponse.RefreshToken);
        var storedToken = await _dbContext.RefreshTokens.FirstAsync(r => r.TokenHash == tokenHash);
        storedToken.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        await _dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<AuthException>(() =>
            _authService.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = registerResponse.RefreshToken }));
    }

    [Fact]
    public async Task LogoutAsync_ShouldRevokeRefreshToken()
    {
        var registerRequest = new RegisterRequest
        {
            Name = "Logout User",
            Email = "logout.user@example.com",
            Password = "Password123"
        };
        var registerResponse = await _authService.RegisterAsync(registerRequest);

        await _authService.LogoutAsync(new LogoutRequest { RefreshToken = registerResponse.RefreshToken });

        // Verify token is revoked
        var tokenHash = _refreshTokenService.HashToken(registerResponse.RefreshToken);
        var storedToken = await _dbContext.RefreshTokens.FirstAsync(r => r.TokenHash == tokenHash);
        Assert.True(storedToken.IsRevoked);

        // Refresh should now fail
        await Assert.ThrowsAsync<AuthException>(() =>
            _authService.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = registerResponse.RefreshToken }));
    }

    public void Dispose()
    {
        _userManager.Dispose();
        _roleManager.Dispose();
        _dbContext.Dispose();
        _serviceProvider.Dispose();
    }
}
