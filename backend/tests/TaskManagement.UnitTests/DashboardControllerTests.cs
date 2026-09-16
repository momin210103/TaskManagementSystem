using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.API.Controllers;
using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs.Dashboard;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using Xunit;

namespace TaskManagement.UnitTests;

public sealed class DashboardControllerTests
{
    private readonly FakeDashboardService _dashboardService = new();
    private readonly DashboardController _controller;

    public DashboardControllerTests()
    {
        _controller = new DashboardController(_dashboardService);
    }

    [Fact]
    public async Task GetSummary_Returns200OK_WithDashboardSummaryResponse()
    {
        _dashboardService.SummaryResult = new DashboardSummaryResponse
        {
            TotalTasks = 15,
            ToDoCount = 5,
            InProgressCount = 6,
            DoneCount = 4
        };

        var result = await _controller.GetSummary(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var summary = Assert.IsType<DashboardSummaryResponse>(okResult.Value);
        Assert.Equal(15, summary.TotalTasks);
    }

    [Fact]
    public async Task GetSummary_WhenAuthException_Returns401Unauthorized()
    {
        _dashboardService.SummaryException = new AuthException("User is not authenticated.");

        var result = await _controller.GetSummary(CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    [Fact]
    public async Task GetSummary_WhenForbiddenException_Returns403Forbidden()
    {
        _dashboardService.SummaryException = new ForbiddenException("Forbidden access");

        var result = await _controller.GetSummary(CancellationToken.None);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task GetTasks_Returns200OK_WithPagedResult()
    {
        _dashboardService.TasksResult = new PagedResult<DashboardTaskResponse>
        {
            Items = [new DashboardTaskResponse { Id = Guid.NewGuid(), Title = "Task 1" }],
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };

        var result = await _controller.GetTasks(new DashboardTaskQueryParameters(), CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var paged = Assert.IsType<PagedResult<DashboardTaskResponse>>(okResult.Value);
        Assert.Single(paged.Items);
    }

    [Fact]
    public async Task GetTasks_WhenValidationException_Returns400BadRequest()
    {
        _dashboardService.TasksException = new ValidationException("Invalid page");

        var result = await _controller.GetTasks(new DashboardTaskQueryParameters { Page = 0 }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task GetTasks_WhenAuthException_Returns401Unauthorized()
    {
        _dashboardService.TasksException = new AuthException("Unauthorized");

        var result = await _controller.GetTasks(new DashboardTaskQueryParameters(), CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    [Fact]
    public async Task GetTasks_WhenForbiddenException_Returns403Forbidden()
    {
        _dashboardService.TasksException = new ForbiddenException("Forbidden team");

        var result = await _controller.GetTasks(new DashboardTaskQueryParameters { TeamId = Guid.NewGuid() }, CancellationToken.None);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
    }

    private sealed class FakeDashboardService : IDashboardService
    {
        public DashboardSummaryResponse? SummaryResult { get; set; }
        public Exception? SummaryException { get; set; }

        public PagedResult<DashboardTaskResponse>? TasksResult { get; set; }
        public Exception? TasksException { get; set; }

        public Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default)
        {
            if (SummaryException is not null)
            {
                throw SummaryException;
            }

            return Task.FromResult(SummaryResult ?? new DashboardSummaryResponse());
        }

        public Task<PagedResult<DashboardTaskResponse>> GetTasksAsync(
            DashboardTaskQueryParameters parameters,
            CancellationToken cancellationToken = default)
        {
            if (TasksException is not null)
            {
                throw TasksException;
            }

            return Task.FromResult(TasksResult ?? new PagedResult<DashboardTaskResponse>());
        }
    }
}

