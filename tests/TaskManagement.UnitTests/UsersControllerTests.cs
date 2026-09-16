using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.API.Controllers;
using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs.Users;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using Xunit;

namespace TaskManagement.UnitTests;

public sealed class UsersControllerTests
{
    private readonly MockUserService _mockUserService = new();
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _controller = new UsersController(_mockUserService);
    }

    [Fact]
    public async Task GetCurrentUser_ReturnsOk_WhenUserFound()
    {
        _mockUserService.CurrentUserResult = new UserResponse { Id = Guid.NewGuid(), Name = "John" };

        var response = await _controller.GetCurrentUser(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response);
        var user = Assert.IsType<UserResponse>(okResult.Value);
        Assert.Equal("John", user.Name);
    }

    [Fact]
    public async Task GetCurrentUser_ReturnsNotFound_WhenUserNotFound()
    {
        _mockUserService.ExceptionToThrow = new NotFoundException("User not found");

        var response = await _controller.GetCurrentUser(CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(response);
        var problem = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
    }

    [Fact]
    public async Task GetUsers_ReturnsOk_WithPagedResult()
    {
        _mockUserService.GetUsersResult = new PagedResult<UserResponse>
        {
            Items = [new UserResponse { Id = Guid.NewGuid() }],
            TotalCount = 1
        };

        var response = await _controller.GetUsers(new UserQueryParameters(), CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response);
        var paged = Assert.IsType<PagedResult<UserResponse>>(okResult.Value);
        Assert.Equal(1, paged.TotalCount);
    }

    [Fact]
    public async Task GetUsers_ReturnsForbidden_WhenForbiddenExceptionThrown()
    {
        _mockUserService.ExceptionToThrow = new ForbiddenException("Denied");

        var response = await _controller.GetUsers(new UserQueryParameters(), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetUserById_ReturnsOk_WhenFound()
    {
        var id = Guid.NewGuid();
        _mockUserService.UserByIdResult = new UserResponse { Id = id, Name = "Alice" };

        var response = await _controller.GetUserById(id, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response);
        var user = Assert.IsType<UserResponse>(okResult.Value);
        Assert.Equal(id, user.Id);
    }

    [Fact]
    public async Task GetUserById_ReturnsNotFound_WhenNotFound()
    {
        _mockUserService.ExceptionToThrow = new NotFoundException("User not found");

        var response = await _controller.GetUserById(Guid.NewGuid(), CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(response);
        var problem = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
    }

    [Fact]
    public async Task GetUserById_ReturnsForbidden_WhenDenied()
    {
        _mockUserService.ExceptionToThrow = new ForbiddenException("Forbidden");

        var response = await _controller.GetUserById(Guid.NewGuid(), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task UpdateUserRole_ReturnsOk_WhenSuccessful()
    {
        var id = Guid.NewGuid();
        _mockUserService.UpdatedRoleResult = new UserResponse { Id = id, Role = "Manager" };

        var response = await _controller.UpdateUserRole(id, new ChangeUserRoleRequest { Role = "Manager" }, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response);
        var user = Assert.IsType<UserResponse>(okResult.Value);
        Assert.Equal("Manager", user.Role);
    }

    [Fact]
    public async Task UpdateUserRole_ReturnsBadRequest_WhenValidationExceptionThrown()
    {
        _mockUserService.ExceptionToThrow = new ValidationException("Invalid role");

        var response = await _controller.UpdateUserRole(Guid.NewGuid(), new ChangeUserRoleRequest { Role = "Invalid" }, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(response);
        var problem = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
    }

    [Fact]
    public async Task UpdateUserRole_ReturnsNotFound_WhenNotFound()
    {
        _mockUserService.ExceptionToThrow = new NotFoundException("Not found");

        var response = await _controller.UpdateUserRole(Guid.NewGuid(), new ChangeUserRoleRequest { Role = "Manager" }, CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(response);
        var problem = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
    }

    [Fact]
    public async Task UpdateUserRole_ReturnsForbidden_WhenForbidden()
    {
        _mockUserService.ExceptionToThrow = new ForbiddenException("Forbidden");

        var response = await _controller.UpdateUserRole(Guid.NewGuid(), new ChangeUserRoleRequest { Role = "Admin" }, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task UpdateUserTeam_ReturnsOk_WhenSuccessful()
    {
        var id = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        _mockUserService.UpdatedTeamResult = new UserResponse { Id = id, TeamId = teamId };

        var response = await _controller.UpdateUserTeam(id, new AssignUserTeamRequest { TeamId = teamId }, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response);
        var user = Assert.IsType<UserResponse>(okResult.Value);
        Assert.Equal(teamId, user.TeamId);
    }

    [Fact]
    public async Task UpdateUserTeam_ReturnsNotFound_WhenNotFound()
    {
        _mockUserService.ExceptionToThrow = new NotFoundException("Team not found");

        var response = await _controller.UpdateUserTeam(Guid.NewGuid(), new AssignUserTeamRequest { TeamId = Guid.NewGuid() }, CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(response);
        var problem = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
    }

    [Fact]
    public async Task UpdateUserTeam_ReturnsForbidden_WhenForbidden()
    {
        _mockUserService.ExceptionToThrow = new ForbiddenException("Forbidden");

        var response = await _controller.UpdateUserTeam(Guid.NewGuid(), new AssignUserTeamRequest { TeamId = Guid.NewGuid() }, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    private sealed class MockUserService : IUserService
    {
        public Exception? ExceptionToThrow { get; set; }
        public UserResponse? CurrentUserResult { get; set; }
        public PagedResult<UserResponse>? GetUsersResult { get; set; }
        public UserResponse? UserByIdResult { get; set; }
        public UserResponse? UpdatedRoleResult { get; set; }
        public UserResponse? UpdatedTeamResult { get; set; }

        public Task<UserResponse> GetCurrentUserAsync(CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow is not null) throw ExceptionToThrow;
            return Task.FromResult(CurrentUserResult ?? new UserResponse());
        }

        public Task<PagedResult<UserResponse>> GetUsersAsync(UserQueryParameters parameters, CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow is not null) throw ExceptionToThrow;
            return Task.FromResult(GetUsersResult ?? new PagedResult<UserResponse>());
        }

        public Task<UserResponse> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow is not null) throw ExceptionToThrow;
            return Task.FromResult(UserByIdResult ?? new UserResponse { Id = id });
        }

        public Task<UserResponse> UpdateUserRoleAsync(Guid id, ChangeUserRoleRequest request, CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow is not null) throw ExceptionToThrow;
            return Task.FromResult(UpdatedRoleResult ?? new UserResponse { Id = id, Role = request.Role });
        }

        public Task<UserResponse> UpdateUserTeamAsync(Guid id, AssignUserTeamRequest request, CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow is not null) throw ExceptionToThrow;
            return Task.FromResult(UpdatedTeamResult ?? new UserResponse { Id = id, TeamId = request.TeamId });
        }
    }
}

