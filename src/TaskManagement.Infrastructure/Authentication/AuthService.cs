using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskManagement.Application.DTOs.Auth;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Identity;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.Infrastructure.Authentication;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenService refreshTokenService,
        IOptions<JwtOptions> jwtOptions)
    {
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(jwtTokenGenerator);
        ArgumentNullException.ThrowIfNull(refreshTokenService);
        ArgumentNullException.ThrowIfNull(jwtOptions);

        _userManager = userManager;
        _dbContext = dbContext;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenService = refreshTokenService;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateRegistrationInput(request);

        var existingUser = await _userManager.FindByEmailAsync(request.Email.Trim()).ConfigureAwait(false);
        if (existingUser is not null)
        {
            throw new DuplicateEmailException("A user with this email address already exists.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            Name = request.Name.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new ValidationException($"Registration failed: {errors}");
        }

        const string defaultRole = nameof(UserRole.User);
        await _userManager.AddToRoleAsync(user, defaultRole).ConfigureAwait(false);

        var (accessToken, rawRefreshToken) = await CreateTokensAndPersistAsync(user, defaultRole, cancellationToken).ConfigureAwait(false);

        return BuildAuthResponse(user, defaultRole, accessToken, rawRefreshToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new AuthException("Invalid email or password.");
        }

        var user = await _userManager.FindByEmailAsync(request.Email.Trim()).ConfigureAwait(false);
        if (user is null)
        {
            throw new AuthException("Invalid email or password.");
        }

        var isValidPassword = await _userManager.CheckPasswordAsync(user, request.Password).ConfigureAwait(false);
        if (!isValidPassword)
        {
            throw new AuthException("Invalid email or password.");
        }

        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        var role = roles.FirstOrDefault() ?? nameof(UserRole.User);

        var (accessToken, rawRefreshToken) = await CreateTokensAndPersistAsync(user, role, cancellationToken).ConfigureAwait(false);

        return BuildAuthResponse(user, role, accessToken, rawRefreshToken);
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new AuthException("Invalid refresh token.");
        }

        var tokenHash = _refreshTokenService.HashToken(request.RefreshToken);

        var storedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash, cancellationToken)
            .ConfigureAwait(false);

        if (storedToken is null || storedToken.IsRevoked || storedToken.IsExpired)
        {
            throw new AuthException("Invalid or expired refresh token.");
        }

        var user = await _userManager.FindByIdAsync(storedToken.UserId.ToString()).ConfigureAwait(false);
        if (user is null)
        {
            throw new AuthException("User associated with token does not exist.");
        }

        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        var role = roles.FirstOrDefault() ?? nameof(UserRole.User);

        var (accessToken, rawRefreshToken) = await RotateRefreshTokenAsync(storedToken, user, role, cancellationToken).ConfigureAwait(false);

        return BuildAuthResponse(user, role, accessToken, rawRefreshToken);
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return;
        }

        var tokenHash = _refreshTokenService.HashToken(request.RefreshToken);

        var storedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash && r.RevokedAt == null, cancellationToken)
            .ConfigureAwait(false);

        if (storedToken is not null)
        {
            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static void ValidateRegistrationInput(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ValidationException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ValidationException("Password is required.");
        }
    }

    private static AuthResponse BuildAuthResponse(ApplicationUser user, string role, string accessToken, string refreshToken)
    {
        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email ?? string.Empty,
                Role = role,
                TeamId = user.TeamId
            }
        };
    }

    private async Task<(string AccessToken, string RawRefreshToken)> RotateRefreshTokenAsync(
        RefreshToken storedToken,
        ApplicationUser user,
        string role,
        CancellationToken cancellationToken)
    {
        var newRawRefreshToken = _refreshTokenService.GenerateRefreshToken();
        var newTokenHash = _refreshTokenService.HashToken(newRawRefreshToken);

        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = newTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.UpdatedAt = DateTime.UtcNow;
        storedToken.ReplacedByTokenId = newRefreshToken.Id;

        _dbContext.RefreshTokens.Add(newRefreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var accessToken = _jwtTokenGenerator.GenerateToken(user.Id, user.Email ?? string.Empty, role);

        return (accessToken, newRawRefreshToken);
    }

    private async Task<(string AccessToken, string RawRefreshToken)> CreateTokensAndPersistAsync(
        ApplicationUser user,
        string role,
        CancellationToken cancellationToken)
    {
        var accessToken = _jwtTokenGenerator.GenerateToken(user.Id, user.Email ?? string.Empty, role);
        var rawRefreshToken = _refreshTokenService.GenerateRefreshToken();
        var tokenHash = _refreshTokenService.HashToken(rawRefreshToken);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return (accessToken, rawRefreshToken);
    }
}

