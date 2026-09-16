using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.API.Controllers;
using TaskManagement.Application.DTOs.Teams;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using Xunit;

namespace TaskManagement.UnitTests;

public sealed class TeamsControllerTests
{
    private readonly MockTeamService _mockTeamService = new();
    private readonly TeamsController _controller;

    public TeamsControllerTests()
    {
        _controller = new TeamsController(_mockTeamService);
    }

    [Fact]
    public async Task CreateTeam_ReturnsCreated_WhenSuccessful()
    {
        var teamId = Guid.NewGuid();
        _mockTeamService.CreateTeamResult = new TeamResponse { Id = teamId, Name = "Alpha" };

        var response = await _controller.CreateTeam(new CreateTeamRequest { Name = "Alpha", ManagerId = Guid.NewGuid() }, CancellationToken.None);

        var createdAtResult = Assert.IsType<CreatedAtActionResult>(response);
        var result = Assert.IsType<TeamResponse>(createdAtResult.Value);
        Assert.Equal(teamId, result.Id);
    }

    [Fact]
    public async Task CreateTeam_ReturnsBadRequest_OnValidationException()
    {
        _mockTeamService.ExceptionToThrow = new ValidationException("Invalid name");

        var response = await _controller.CreateTeam(new CreateTeamRequest(), CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(response);
        var problem = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
    }

    [Fact]
    public async Task CreateTeam_ReturnsConflict_OnConflictException()
    {
        _mockTeamService.ExceptionToThrow = new ConflictException("Already exists");

        var response = await _controller.CreateTeam(new CreateTeamRequest(), CancellationToken.None);

        var conflictResult = Assert.IsType<ConflictObjectResult>(response);
        var problem = Assert.IsType<ProblemDetails>(conflictResult.Value);
        Assert.Equal(StatusCodes.Status409Conflict, problem.Status);
    }

    [Fact]
    public async Task CreateTeam_ReturnsForbidden_OnForbiddenException()
    {
        _mockTeamService.ExceptionToThrow = new ForbiddenException("Denied");

        var response = await _controller.CreateTeam(new CreateTeamRequest(), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetTeams_ReturnsOk_WithTeamsList()
    {
        _mockTeamService.GetTeamsResult = [new TeamResponse { Id = Guid.NewGuid(), Name = "Dev" }];

        var response = await _controller.GetTeams(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response);
        var teams = Assert.IsAssignableFrom<IReadOnlyList<TeamResponse>>(okResult.Value);
        Assert.Single(teams);
    }

    [Fact]
    public async Task GetTeamById_ReturnsOk_WhenFound()
    {
        var teamId = Guid.NewGuid();
        _mockTeamService.GetTeamByIdResult = new TeamDetailsResponse { Id = teamId, Name = "Dev" };

        var response = await _controller.GetTeamById(teamId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response);
        var team = Assert.IsType<TeamDetailsResponse>(okResult.Value);
        Assert.Equal(teamId, team.Id);
    }

    [Fact]
    public async Task GetTeamById_ReturnsNotFound_WhenNotFound()
    {
        _mockTeamService.ExceptionToThrow = new NotFoundException("Not found");

        var response = await _controller.GetTeamById(Guid.NewGuid(), CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(response);
        var problem = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
    }

    [Fact]
    public async Task UpdateTeam_ReturnsOk_WhenSuccessful()
    {
        var teamId = Guid.NewGuid();
        _mockTeamService.UpdateTeamResult = new TeamResponse { Id = teamId, Name = "Updated" };

        var response = await _controller.UpdateTeam(teamId, new UpdateTeamRequest { Name = "Updated" }, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response);
        var team = Assert.IsType<TeamResponse>(okResult.Value);
        Assert.Equal("Updated", team.Name);
    }

    [Fact]
    public async Task AddMember_ReturnsOk_WhenSuccessful()
    {
        var teamId = Guid.NewGuid();
        _mockTeamService.AddMemberResult = new TeamDetailsResponse { Id = teamId, Name = "Dev" };

        var response = await _controller.AddMember(teamId, new AddTeamMemberRequest { UserId = Guid.NewGuid() }, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response);
        var team = Assert.IsType<TeamDetailsResponse>(okResult.Value);
        Assert.Equal(teamId, team.Id);
    }

    [Fact]
    public async Task AddMemberByRoute_ReturnsOk_WhenSuccessful()
    {
        var teamId = Guid.NewGuid();
        _mockTeamService.AddMemberResult = new TeamDetailsResponse { Id = teamId, Name = "Dev" };

        var response = await _controller.AddMemberByRoute(teamId, Guid.NewGuid(), CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response);
        var team = Assert.IsType<TeamDetailsResponse>(okResult.Value);
        Assert.Equal(teamId, team.Id);
    }

    [Fact]
    public async Task RemoveMember_ReturnsOk_WhenSuccessful()
    {
        var teamId = Guid.NewGuid();
        _mockTeamService.RemoveMemberResult = new TeamDetailsResponse { Id = teamId, Name = "Dev" };

        var response = await _controller.RemoveMember(teamId, Guid.NewGuid(), CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response);
        var team = Assert.IsType<TeamDetailsResponse>(okResult.Value);
        Assert.Equal(teamId, team.Id);
    }

    private sealed class MockTeamService : ITeamService
    {
        public Exception? ExceptionToThrow { get; set; }
        public TeamResponse? CreateTeamResult { get; set; }
        public IReadOnlyList<TeamResponse> GetTeamsResult { get; set; } = [];
        public TeamDetailsResponse? GetTeamByIdResult { get; set; }
        public TeamResponse? UpdateTeamResult { get; set; }
        public TeamDetailsResponse? AddMemberResult { get; set; }
        public TeamDetailsResponse? RemoveMemberResult { get; set; }

        public Task<TeamResponse> CreateTeamAsync(CreateTeamRequest request, CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }
            return Task.FromResult(CreateTeamResult ?? new TeamResponse());
        }

        public Task<IReadOnlyList<TeamResponse>> GetTeamsAsync(CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }
            return Task.FromResult(GetTeamsResult);
        }

        public Task<TeamDetailsResponse> GetTeamByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }
            return Task.FromResult(GetTeamByIdResult ?? new TeamDetailsResponse());
        }

        public Task<TeamResponse> UpdateTeamAsync(Guid id, UpdateTeamRequest request, CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }
            return Task.FromResult(UpdateTeamResult ?? new TeamResponse());
        }

        public Task<TeamDetailsResponse> AddMemberAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }
            return Task.FromResult(AddMemberResult ?? new TeamDetailsResponse());
        }

        public Task<TeamDetailsResponse> RemoveMemberAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }
            return Task.FromResult(RemoveMemberResult ?? new TeamDetailsResponse());
        }
    }
}

