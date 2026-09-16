using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Application.DTOs.Dashboard;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Identity;
using TaskManagement.Infrastructure.Persistence;
using TaskManagement.Infrastructure.Persistence.Repositories;
using Xunit;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.UnitTests;

public sealed class DashboardAuthorizationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly DashboardRepository _dashboardRepository;
    private readonly TestCurrentUserService _currentUserService;
    private readonly DashboardService _dashboardService;

    public DashboardAuthorizationTests()
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

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        _userManager = _serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        _dashboardRepository = new DashboardRepository(_dbContext);
        _currentUserService = new TestCurrentUserService();
        _dashboardService = new DashboardService(_dashboardRepository, _currentUserService);

        DatabaseSeeder.SeedAsync(_serviceProvider).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task Admin_GetSummary_ReturnsSystemWideSummary()
    {
        var admin = await _userManager.FindByEmailAsync("admin@taskmanagement.com");
        Assert.NotNull(admin);

        _currentUserService.SetUser(admin.Id, admin.Email!, UserRole.Admin.ToString());

        var totalTasksInDb = await _dbContext.Tasks.CountAsync();
        var summary = await _dashboardService.GetSummaryAsync();

        Assert.NotNull(summary);
        Assert.Equal(totalTasksInDb, summary.TotalTasks);
        Assert.True(summary.TotalTasks >= 3);
    }

    [Fact]
    public async Task Manager_GetSummary_ReturnsOwnTeamSummaryOnly()
    {
        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        var engTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "Engineering Team");
        Assert.NotNull(manager1);
        Assert.NotNull(engTeam);

        _currentUserService.SetUser(manager1.Id, manager1.Email!, UserRole.Manager.ToString());

        var engTasksCount = await _dbContext.Tasks.CountAsync(t => t.TeamId == engTeam.Id);
        var summary = await _dashboardService.GetSummaryAsync();

        Assert.NotNull(summary);
        Assert.Equal(engTasksCount, summary.TotalTasks);
    }

    [Fact]
    public async Task User_GetSummary_ReturnsAssignedTasksSummaryOnly()
    {
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        Assert.NotNull(dev1);

        _currentUserService.SetUser(dev1.Id, dev1.Email!, UserRole.User.ToString());

        var dev1TasksCount = await _dbContext.Tasks.CountAsync(t => t.AssignedToId == dev1.Id);
        var summary = await _dashboardService.GetSummaryAsync();

        Assert.NotNull(summary);
        Assert.Equal(dev1TasksCount, summary.TotalTasks);
    }

    [Fact]
    public async Task SummaryCalculations_OverdueDueTodayAndUpcoming_HandledCorrectly()
    {
        var admin = await _userManager.FindByEmailAsync("admin@taskmanagement.com");
        var team = await _dbContext.Teams.FirstOrDefaultAsync();
        Assert.NotNull(admin);
        Assert.NotNull(team);

        var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);

        // Clear existing tasks for isolated calculation test
        _dbContext.Tasks.RemoveRange(_dbContext.Tasks);
        await _dbContext.SaveChangesAsync();

        var tasks = new List<TaskItem>
        {
            // Overdue open task
            new() { Id = Guid.NewGuid(), Title = "Overdue Open", Status = TaskStatus.ToDo, Priority = TaskPriority.High, Deadline = todayUtc.AddDays(-2), TeamId = team.Id, AssignedToId = admin.Id, AssignedById = admin.Id },
            // Overdue DONE task (should NOT be counted in OverdueCount)
            new() { Id = Guid.NewGuid(), Title = "Overdue Done", Status = TaskStatus.Done, Priority = TaskPriority.Medium, Deadline = todayUtc.AddDays(-2), TeamId = team.Id, AssignedToId = admin.Id, AssignedById = admin.Id },
            // Due Today open task
            new() { Id = Guid.NewGuid(), Title = "Due Today Open", Status = TaskStatus.InProgress, Priority = TaskPriority.High, Deadline = todayUtc, TeamId = team.Id, AssignedToId = admin.Id, AssignedById = admin.Id },
            // Due Today DONE task (should NOT be counted in DueTodayCount)
            new() { Id = Guid.NewGuid(), Title = "Due Today Done", Status = TaskStatus.Done, Priority = TaskPriority.Low, Deadline = todayUtc, TeamId = team.Id, AssignedToId = admin.Id, AssignedById = admin.Id },
            // Upcoming open task
            new() { Id = Guid.NewGuid(), Title = "Upcoming Open", Status = TaskStatus.ToDo, Priority = TaskPriority.Medium, Deadline = todayUtc.AddDays(3), TeamId = team.Id, AssignedToId = admin.Id, AssignedById = admin.Id },
            // Upcoming DONE task (should NOT be counted in UpcomingCount)
            new() { Id = Guid.NewGuid(), Title = "Upcoming Done", Status = TaskStatus.Done, Priority = TaskPriority.Low, Deadline = todayUtc.AddDays(3), TeamId = team.Id, AssignedToId = admin.Id, AssignedById = admin.Id }
        };

        _dbContext.Tasks.AddRange(tasks);
        await _dbContext.SaveChangesAsync();

        _currentUserService.SetUser(admin.Id, admin.Email!, UserRole.Admin.ToString());

        var summary = await _dashboardService.GetSummaryAsync();

        Assert.NotNull(summary);
        Assert.Equal(6, summary.TotalTasks);
        Assert.Equal(2, summary.ToDoCount);
        Assert.Equal(1, summary.InProgressCount);
        Assert.Equal(3, summary.DoneCount);
        Assert.Equal(2, summary.HighPriorityCount);
        Assert.Equal(1, summary.OverdueCount);
        Assert.Equal(1, summary.DueTodayCount);
        Assert.Equal(1, summary.UpcomingCount);
    }

    [Fact]
    public async Task Admin_GetTasks_CanFilterByAnyTeamAndAssignee()
    {
        var admin = await _userManager.FindByEmailAsync("admin@taskmanagement.com");
        var engTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "Engineering Team");
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        Assert.NotNull(admin);
        Assert.NotNull(engTeam);
        Assert.NotNull(dev1);

        _currentUserService.SetUser(admin.Id, admin.Email!, UserRole.Admin.ToString());

        var result = await _dashboardService.GetTasksAsync(new DashboardTaskQueryParameters
        {
            TeamId = engTeam.Id,
            AssignedToId = dev1.Id
        });

        Assert.NotNull(result);
        Assert.All(result.Items, t =>
        {
            Assert.Equal(engTeam.Id, t.TeamId);
            Assert.Equal(dev1.Id, t.AssignedToId);
        });
    }

    [Fact]
    public async Task Manager_GetTasks_ScopedToOwnTeam()
    {
        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        var engTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "Engineering Team");
        Assert.NotNull(manager1);
        Assert.NotNull(engTeam);

        _currentUserService.SetUser(manager1.Id, manager1.Email!, UserRole.Manager.ToString());

        var result = await _dashboardService.GetTasksAsync(new DashboardTaskQueryParameters());

        Assert.NotNull(result);
        Assert.All(result.Items, t => Assert.Equal(engTeam.Id, t.TeamId));
    }

    [Fact]
    public async Task Manager_GetTasks_ForOtherTeam_ThrowsForbidden_IDOR()
    {
        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        var qaTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "QA & Testing Team");
        Assert.NotNull(manager1);
        Assert.NotNull(qaTeam);

        _currentUserService.SetUser(manager1.Id, manager1.Email!, UserRole.Manager.ToString());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _dashboardService.GetTasksAsync(new DashboardTaskQueryParameters { TeamId = qaTeam.Id }));
    }

    [Fact]
    public async Task User_GetTasks_ScopedToOwnAssignedTasks_IDOR()
    {
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        var dev2 = await _userManager.FindByEmailAsync("dev2@taskmanagement.com");
        Assert.NotNull(dev1);
        Assert.NotNull(dev2);

        _currentUserService.SetUser(dev1.Id, dev1.Email!, UserRole.User.ToString());

        // Attempting to filter by another user's assignedToId should ignore the override and return only dev1 tasks
        var result = await _dashboardService.GetTasksAsync(new DashboardTaskQueryParameters { AssignedToId = dev2.Id });

        Assert.NotNull(result);
        Assert.All(result.Items, t => Assert.Equal(dev1.Id, t.AssignedToId));
    }

    [Fact]
    public async Task GetTasks_FiltersByStatusPriorityAndDateRange_Works()
    {
        var admin = await _userManager.FindByEmailAsync("admin@taskmanagement.com");
        Assert.NotNull(admin);

        _currentUserService.SetUser(admin.Id, admin.Email!, UserRole.Admin.ToString());

        var result = await _dashboardService.GetTasksAsync(new DashboardTaskQueryParameters
        {
            Status = "ToDo",
            Priority = "High",
            DeadlineFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            DeadlineTo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            SortBy = "deadline",
            SortDirection = "asc"
        });

        Assert.NotNull(result);
        Assert.All(result.Items, t =>
        {
            Assert.Equal("ToDo", t.Status);
            Assert.Equal("High", t.Priority);
        });
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _userManager.Dispose();
        _serviceProvider.Dispose();
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

        public bool IsInRole(string role)
        {
            return string.Equals(Role, role, StringComparison.OrdinalIgnoreCase);
        }
    }
}

