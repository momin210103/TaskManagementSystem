using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs.Dashboard;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Enums;
using Xunit;

namespace TaskManagement.UnitTests;

public sealed class DashboardServiceTests
{
    private readonly TestDashboardRepository _dashboardRepository = new();
    private readonly TestCurrentUserService _currentUserService = new();
    private readonly DashboardService _dashboardService;

    public DashboardServiceTests()
    {
        _dashboardService = new DashboardService(_dashboardRepository, _currentUserService);
    }

    [Fact]
    public async Task GetSummaryAsync_WhenUnauthenticated_ThrowsAuthException()
    {
        _currentUserService.ClearUser();

        await Assert.ThrowsAsync<AuthException>(() =>
            _dashboardService.GetSummaryAsync());
    }

    [Fact]
    public async Task GetSummaryAsync_AsAdmin_QueriesGlobalSummary()
    {
        var adminId = Guid.NewGuid();
        _currentUserService.SetUser(adminId, "admin@test.com", UserRole.Admin.ToString());

        _dashboardRepository.SummaryToReturn = new DashboardSummaryResponse
        {
            TotalTasks = 10,
            ToDoCount = 4,
            InProgressCount = 3,
            DoneCount = 3
        };

        var result = await _dashboardService.GetSummaryAsync();

        Assert.NotNull(result);
        Assert.Equal(10, result.TotalTasks);
        Assert.Null(_dashboardRepository.LastEnforcedTeamId);
        Assert.Null(_dashboardRepository.LastEnforcedAssignedToId);
    }

    [Fact]
    public async Task GetSummaryAsync_AsManager_QueriesTeamScopedSummary()
    {
        var managerId = Guid.NewGuid();
        var teamId = Guid.NewGuid();

        _currentUserService.SetUser(managerId, "manager@test.com", UserRole.Manager.ToString());
        _dashboardRepository.ManagerTeamMap[managerId] = teamId;

        _dashboardRepository.SummaryToReturn = new DashboardSummaryResponse { TotalTasks = 5 };

        var result = await _dashboardService.GetSummaryAsync();

        Assert.NotNull(result);
        Assert.Equal(5, result.TotalTasks);
        Assert.Equal(teamId, _dashboardRepository.LastEnforcedTeamId);
        Assert.Null(_dashboardRepository.LastEnforcedAssignedToId);
    }

    [Fact]
    public async Task GetSummaryAsync_AsManagerWithoutTeam_ReturnsEmptySummary()
    {
        var managerId = Guid.NewGuid();
        _currentUserService.SetUser(managerId, "manager@test.com", UserRole.Manager.ToString());

        var result = await _dashboardService.GetSummaryAsync();

        Assert.NotNull(result);
        Assert.Equal(0, result.TotalTasks);
    }

    [Fact]
    public async Task GetSummaryAsync_AsUser_QueriesUserAssignedSummary()
    {
        var userId = Guid.NewGuid();
        _currentUserService.SetUser(userId, "user@test.com", UserRole.User.ToString());

        _dashboardRepository.SummaryToReturn = new DashboardSummaryResponse { TotalTasks = 2 };

        var result = await _dashboardService.GetSummaryAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalTasks);
        Assert.Null(_dashboardRepository.LastEnforcedTeamId);
        Assert.Equal(userId, _dashboardRepository.LastEnforcedAssignedToId);
    }

    [Fact]
    public async Task GetTasksAsync_WhenUnauthenticated_ThrowsAuthException()
    {
        _currentUserService.ClearUser();

        await Assert.ThrowsAsync<AuthException>(() =>
            _dashboardService.GetTasksAsync(new DashboardTaskQueryParameters()));
    }

    [Fact]
    public async Task GetTasksAsync_WithInvalidPage_ThrowsValidationException()
    {
        _currentUserService.SetUser(Guid.NewGuid(), "admin@test.com", UserRole.Admin.ToString());

        await Assert.ThrowsAsync<ValidationException>(() =>
            _dashboardService.GetTasksAsync(new DashboardTaskQueryParameters { Page = 0 }));
    }

    [Fact]
    public async Task GetTasksAsync_WithInvalidPageSize_ThrowsValidationException()
    {
        _currentUserService.SetUser(Guid.NewGuid(), "admin@test.com", UserRole.Admin.ToString());

        await Assert.ThrowsAsync<ValidationException>(() =>
            _dashboardService.GetTasksAsync(new DashboardTaskQueryParameters { PageSize = 101 }));
    }

    [Fact]
    public async Task GetTasksAsync_WithInvalidDateRange_ThrowsValidationException()
    {
        _currentUserService.SetUser(Guid.NewGuid(), "admin@test.com", UserRole.Admin.ToString());

        var parameters = new DashboardTaskQueryParameters
        {
            DeadlineFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            DeadlineTo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))
        };

        await Assert.ThrowsAsync<ValidationException>(() =>
            _dashboardService.GetTasksAsync(parameters));
    }

    [Fact]
    public async Task GetTasksAsync_WithInvalidSortBy_ThrowsValidationException()
    {
        _currentUserService.SetUser(Guid.NewGuid(), "admin@test.com", UserRole.Admin.ToString());

        var parameters = new DashboardTaskQueryParameters { SortBy = "drop_table" };

        await Assert.ThrowsAsync<ValidationException>(() =>
            _dashboardService.GetTasksAsync(parameters));
    }

    [Fact]
    public async Task GetTasksAsync_WithInvalidSortDirection_ThrowsValidationException()
    {
        _currentUserService.SetUser(Guid.NewGuid(), "admin@test.com", UserRole.Admin.ToString());

        var parameters = new DashboardTaskQueryParameters { SortDirection = "sideways" };

        await Assert.ThrowsAsync<ValidationException>(() =>
            _dashboardService.GetTasksAsync(parameters));
    }

    [Fact]
    public async Task GetTasksAsync_AsManager_WithOtherTeamFilter_ThrowsForbiddenException()
    {
        var managerId = Guid.NewGuid();
        var ownTeamId = Guid.NewGuid();
        var otherTeamId = Guid.NewGuid();

        _currentUserService.SetUser(managerId, "manager@test.com", UserRole.Manager.ToString());
        _dashboardRepository.ManagerTeamMap[managerId] = ownTeamId;

        var parameters = new DashboardTaskQueryParameters { TeamId = otherTeamId };

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _dashboardService.GetTasksAsync(parameters));
    }

    [Fact]
    public async Task GetTasksAsync_AsUser_OverridesAssignedToIdWithCurrentUserId()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        _currentUserService.SetUser(userId, "user@test.com", UserRole.User.ToString());

        var parameters = new DashboardTaskQueryParameters { AssignedToId = otherUserId };

        var result = await _dashboardService.GetTasksAsync(parameters);

        Assert.NotNull(result);
        Assert.Equal(userId, _dashboardRepository.LastEnforcedAssignedToId);
    }

    [Fact]
    public async Task GetTasksAsync_AsAdmin_ReturnsTasks()
    {
        var adminId = Guid.NewGuid();
        _currentUserService.SetUser(adminId, "admin@test.com", UserRole.Admin.ToString());
        _dashboardRepository.TasksToReturn = new PagedResult<DashboardTaskResponse>
        {
            Items = [new DashboardTaskResponse { Id = Guid.NewGuid(), Title = "Task 1" }],
            TotalCount = 1
        };

        var result = await _dashboardService.GetTasksAsync(new DashboardTaskQueryParameters());

        Assert.NotNull(result);
        Assert.Single(result.Items);
    }

    private sealed class TestDashboardRepository : IDashboardRepository
    {
        public DashboardSummaryResponse? SummaryToReturn { get; set; }
        public PagedResult<DashboardTaskResponse>? TasksToReturn { get; set; }
        public Dictionary<Guid, Guid> ManagerTeamMap { get; } = new();

        public Guid? LastEnforcedTeamId { get; private set; }
        public Guid? LastEnforcedAssignedToId { get; private set; }

        public Task<DashboardSummaryResponse> GetSummaryAsync(
            Guid? enforcedTeamId,
            Guid? enforcedAssignedToId,
            DateOnly todayUtc,
            CancellationToken cancellationToken = default)
        {
            LastEnforcedTeamId = enforcedTeamId;
            LastEnforcedAssignedToId = enforcedAssignedToId;
            return Task.FromResult(SummaryToReturn ?? new DashboardSummaryResponse());
        }

        public Task<PagedResult<DashboardTaskResponse>> GetTasksPagedAsync(
            DashboardTaskQueryParameters parameters,
            Guid? enforcedTeamId,
            Guid? enforcedAssignedToId,
            CancellationToken cancellationToken = default)
        {
            LastEnforcedTeamId = enforcedTeamId;
            LastEnforcedAssignedToId = enforcedAssignedToId;
            return Task.FromResult(TasksToReturn ?? new PagedResult<DashboardTaskResponse>());
        }

        public Task<Guid?> GetManagedTeamIdAsync(Guid managerId, CancellationToken cancellationToken = default)
        {
            if (ManagerTeamMap.TryGetValue(managerId, out var teamId))
            {
                return Task.FromResult<Guid?>(teamId);
            }
            return Task.FromResult<Guid?>(null);
        }
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public Guid? UserId { get; private set; }
        public string? Email { get; private set; }
        public string? Role { get; private set; }
        public bool IsAuthenticated => UserId.HasValue;

        public void SetUser(Guid userId, string email, string role)
        {
            UserId = userId;
            Email = email;
            Role = role;
        }

        public void ClearUser()
        {
            UserId = null;
            Email = null;
            Role = null;
        }

        public bool IsInRole(string role)
        {
            return string.Equals(Role, role, StringComparison.OrdinalIgnoreCase);
        }
    }
}
