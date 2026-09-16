using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.API.Controllers;
using TaskManagement.Application.DTOs.Auth;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.UnitTests;

public class AuthControllerTests
{
    private sealed class FakeAuthService : IAuthService
    {
        public Func<RegisterRequest, Task<AuthResponse>>? OnRegister { get; set; }
        public Func<LoginRequest, Task<AuthResponse>>? OnLogin { get; set; }
        public Func<RefreshTokenRequest, Task<AuthResponse>>? OnRefresh { get; set; }
        public Func<LogoutRequest, Task>? OnLogout { get; set; }

        public Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
        {
            return OnRegister != null ? OnRegister(request) : Task.FromResult(new AuthResponse());
        }

        public Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            return OnLogin != null ? OnLogin(request) : Task.FromResult(new AuthResponse());
        }

        public Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
        {
            return OnRefresh != null ? OnRefresh(request) : Task.FromResult(new AuthResponse());
        }

        public Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default)
        {
            return OnLogout != null ? OnLogout(request) : Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Register_Success_ShouldReturnOkWithAuthResponse()
    {
        var fakeService = new FakeAuthService
        {
            OnRegister = req => Task.FromResult(new AuthResponse
            {
                AccessToken = "access-token",
                RefreshToken = "refresh-token",
                User = new UserDto { Name = req.Name, Email = req.Email, Role = "User" }
            })
        };
        var controller = new AuthController(fakeService);

        var result = await controller.Register(new RegisterRequest
        {
            Name = "Alice",
            Email = "alice@example.com",
            Password = "Password123"
        }, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponse>(okResult.Value);
        Assert.Equal("access-token", response.AccessToken);
        Assert.Equal("alice@example.com", response.User.Email);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ShouldReturnConflict()
    {
        var fakeService = new FakeAuthService
        {
            OnRegister = _ => throw new DuplicateEmailException("Email exists")
        };
        var controller = new AuthController(fakeService);

        var result = await controller.Register(new RegisterRequest
        {
            Name = "Alice",
            Email = "alice@example.com",
            Password = "Password123"
        }, CancellationToken.None);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(conflictResult.Value);
        Assert.Equal(StatusCodes.Status409Conflict, problemDetails.Status);
    }

    [Fact]
    public async Task Register_ValidationError_ShouldReturnBadRequest()
    {
        var fakeService = new FakeAuthService
        {
            OnRegister = _ => throw new ValidationException("Invalid input")
        };
        var controller = new AuthController(fakeService);

        var result = await controller.Register(new RegisterRequest(), CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problemDetails.Status);
    }

    [Fact]
    public async Task Login_Success_ShouldReturnOkWithAuthResponse()
    {
        var fakeService = new FakeAuthService
        {
            OnLogin = req => Task.FromResult(new AuthResponse
            {
                AccessToken = "access-token",
                RefreshToken = "refresh-token",
                User = new UserDto { Email = req.Email, Role = "User" }
            })
        };
        var controller = new AuthController(fakeService);

        var result = await controller.Login(new LoginRequest
        {
            Email = "alice@example.com",
            Password = "Password123"
        }, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponse>(okResult.Value);
        Assert.Equal("access-token", response.AccessToken);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ShouldReturnUnauthorized()
    {
        var fakeService = new FakeAuthService
        {
            OnLogin = _ => throw new AuthException("Invalid credentials")
        };
        var controller = new AuthController(fakeService);

        var result = await controller.Login(new LoginRequest
        {
            Email = "alice@example.com",
            Password = "WrongPassword"
        }, CancellationToken.None);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(unauthorizedResult.Value);
        Assert.Equal(StatusCodes.Status401Unauthorized, problemDetails.Status);
    }

    [Fact]
    public async Task Refresh_InvalidToken_ShouldReturnUnauthorized()
    {
        var fakeService = new FakeAuthService
        {
            OnRefresh = _ => throw new AuthException("Invalid refresh token")
        };
        var controller = new AuthController(fakeService);

        var result = await controller.Refresh(new RefreshTokenRequest { RefreshToken = "invalid" }, CancellationToken.None);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(unauthorizedResult.Value);
        Assert.Equal(StatusCodes.Status401Unauthorized, problemDetails.Status);
    }

    [Fact]
    public async Task Logout_ShouldReturnOk()
    {
        var logoutCalled = false;
        var fakeService = new FakeAuthService
        {
            OnLogout = _ =>
            {
                logoutCalled = true;
                return Task.CompletedTask;
            }
        };
        var controller = new AuthController(fakeService);

        var result = await controller.Logout(new LogoutRequest { RefreshToken = "valid-token" }, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.True(logoutCalled);
    }
}

