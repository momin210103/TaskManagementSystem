using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Identity;
using TaskManagement.Infrastructure.Persistence;
using TaskManagement.Infrastructure.Persistence.Repositories;
using Xunit;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.UnitTests;

public sealed class TasksAuthorizationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TaskRepository _taskRepository;
    private readonly TestCurrentUserService _currentUserService;
    private readonly TaskService _taskService;

    public TasksAuthorizationTests()
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

        _taskRepository = new TaskRepository(_dbContext);
        _currentUserService = new TestCurrentUserService();
        _taskService = new TaskService(_taskRepository, _currentUserService);

        DatabaseSeeder.SeedAsync(_serviceProvider).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task Admin_CreateTask_Succeeds()
    {
        var admin = await _userManager.FindByEmailAsync("admin@taskmanagement.com");
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        var engTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "Engineering Team");
        Assert.NotNull(admin);
        Assert.NotNull(dev1);
        Assert.NotNull(engTeam);

        _currentUserService.SetUser(admin.Id, admin.Email!, UserRole.Admin.ToString());

        var result = await _taskService.CreateTaskAsync(new CreateTaskRequest
        {
            Title = "Admin Created Task",
            Description = "Admin description",
            Status = TaskStatus.ToDo,
            Priority = TaskPriority.High,
            Deadline = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            TeamId = engTeam.Id,
            AssignedToId = dev1.Id
        });

        Assert.NotNull(result);
        Assert.Equal("Admin Created Task", result.Title);
        Assert.Equal(admin.Id, result.AssignedById);
        Assert.Equal(admin.Name, result.AssignedByName);
    }

    [Fact]
    public async Task Manager_CreateTask_ForOwnTeam_Succeeds()
    {
        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        var engTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "Engineering Team");
        Assert.NotNull(manager1);
        Assert.NotNull(dev1);
        Assert.NotNull(engTeam);

        _currentUserService.SetUser(manager1.Id, manager1.Email!, UserRole.Manager.ToString());

        var result = await _taskService.CreateTaskAsync(new CreateTaskRequest
        {
            Title = "Engineering Sprint Task",
            Deadline = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            TeamId = engTeam.Id,
            AssignedToId = dev1.Id
        });

        Assert.NotNull(result);
        Assert.Equal(manager1.Id, result.AssignedById);
    }

    [Fact]
    public async Task Manager_CreateTask_ForOtherTeam_ThrowsForbidden()
    {
        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        var qa1 = await _userManager.FindByEmailAsync("qa1@taskmanagement.com");
        var qaTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "QA & Testing Team");
        Assert.NotNull(manager1);
        Assert.NotNull(qa1);
        Assert.NotNull(qaTeam);

        _currentUserService.SetUser(manager1.Id, manager1.Email!, UserRole.Manager.ToString());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _taskService.CreateTaskAsync(new CreateTaskRequest
            {
                Title = "Cross Team Task",
                Deadline = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
                TeamId = qaTeam.Id,
                AssignedToId = qa1.Id
            }));
    }

    [Fact]
    public async Task User_CreateTask_ThrowsForbidden()
    {
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        var engTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "Engineering Team");
        Assert.NotNull(dev1);
        Assert.NotNull(engTeam);

        _currentUserService.SetUser(dev1.Id, dev1.Email!, UserRole.User.ToString());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _taskService.CreateTaskAsync(new CreateTaskRequest
            {
                Title = "User Attempt Task",
                Deadline = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
                TeamId = engTeam.Id,
                AssignedToId = dev1.Id
            }));
    }

    [Fact]
    public async Task Admin_GetTasks_ReturnsAllTasks()
    {
        var admin = await _userManager.FindByEmailAsync("admin@taskmanagement.com");
        Assert.NotNull(admin);

        _currentUserService.SetUser(admin.Id, admin.Email!, UserRole.Admin.ToString());

        var result = await _taskService.GetTasksAsync(new TaskQueryParameters { Page = 1, PageSize = 50 });

        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 3);
    }

    [Fact]
    public async Task Manager_GetTasks_ReturnsOnlyOwnTeamTasks()
    {
        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        var engTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "Engineering Team");
        Assert.NotNull(manager1);
        Assert.NotNull(engTeam);

        _currentUserService.SetUser(manager1.Id, manager1.Email!, UserRole.Manager.ToString());

        var result = await _taskService.GetTasksAsync(new TaskQueryParameters());

        Assert.NotNull(result);
        Assert.All(result.Items, t => Assert.Equal(engTeam.Id, t.TeamId));
    }

    [Fact]
    public async Task User_GetTasks_ReturnsOnlyAssignedTasks()
    {
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        Assert.NotNull(dev1);

        _currentUserService.SetUser(dev1.Id, dev1.Email!, UserRole.User.ToString());

        var result = await _taskService.GetTasksAsync(new TaskQueryParameters());

        Assert.NotNull(result);
        Assert.All(result.Items, t => Assert.Equal(dev1.Id, t.AssignedToId));
    }

    [Fact]
    public async Task User_GetTasks_WithOtherUserFilter_IgnoresFilterAndReturnsOwnTasks_IDOR()
    {
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        var dev2 = await _userManager.FindByEmailAsync("dev2@taskmanagement.com");
        Assert.NotNull(dev1);
        Assert.NotNull(dev2);

        _currentUserService.SetUser(dev1.Id, dev1.Email!, UserRole.User.ToString());

        var result = await _taskService.GetTasksAsync(new TaskQueryParameters { AssignedToId = dev2.Id });

        Assert.NotNull(result);
        Assert.All(result.Items, t => Assert.Equal(dev1.Id, t.AssignedToId));
    }

    [Fact]
    public async Task User_GetTaskById_ForOtherUserTask_ThrowsForbidden_IDOR()
    {
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        var dev2 = await _userManager.FindByEmailAsync("dev2@taskmanagement.com");
        var dev2Task = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.AssignedToId == dev2!.Id);
        Assert.NotNull(dev1);
        Assert.NotNull(dev2Task);

        _currentUserService.SetUser(dev1.Id, dev1.Email!, UserRole.User.ToString());

        await Assert.ThrowsAsync<ForbiddenException>(() => _taskService.GetTaskByIdAsync(dev2Task.Id));
    }

    [Fact]
    public async Task Manager_GetTaskById_ForOtherTeamTask_ThrowsForbidden_IDOR()
    {
        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        var qaTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "QA & Testing Team");
        var qaTask = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.TeamId == qaTeam!.Id);
        Assert.NotNull(manager1);
        Assert.NotNull(qaTask);

        _currentUserService.SetUser(manager1.Id, manager1.Email!, UserRole.Manager.ToString());

        await Assert.ThrowsAsync<ForbiddenException>(() => _taskService.GetTaskByIdAsync(qaTask.Id));
    }

    [Fact]
    public async Task Admin_UpdateTask_Succeeds()
    {
        var admin = await _userManager.FindByEmailAsync("admin@taskmanagement.com");
        var anyTask = await _dbContext.Tasks.FirstOrDefaultAsync();
        Assert.NotNull(admin);
        Assert.NotNull(anyTask);

        _currentUserService.SetUser(admin.Id, admin.Email!, UserRole.Admin.ToString());

        var result = await _taskService.UpdateTaskAsync(anyTask.Id, new UpdateTaskRequest
        {
            Title = "Admin Updated Title",
            Priority = TaskPriority.High
        });

        Assert.NotNull(result);
        Assert.Equal("Admin Updated Title", result.Title);
        Assert.Equal(TaskPriority.High.ToString(), result.Priority);
    }

    [Fact]
    public async Task Manager_UpdateTask_ForOwnTeam_Succeeds()
    {
        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        var engTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "Engineering Team");
        var engTask = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.TeamId == engTeam!.Id);
        Assert.NotNull(manager1);
        Assert.NotNull(engTask);

        _currentUserService.SetUser(manager1.Id, manager1.Email!, UserRole.Manager.ToString());

        var result = await _taskService.UpdateTaskAsync(engTask.Id, new UpdateTaskRequest
        {
            Title = "Manager Updated Title"
        });

        Assert.NotNull(result);
        Assert.Equal("Manager Updated Title", result.Title);
    }

    [Fact]
    public async Task Manager_UpdateTask_ForOtherTeam_ThrowsForbidden_IDOR()
    {
        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        var qaTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "QA & Testing Team");
        var qaTask = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.TeamId == qaTeam!.Id);
        Assert.NotNull(manager1);
        Assert.NotNull(qaTask);

        _currentUserService.SetUser(manager1.Id, manager1.Email!, UserRole.Manager.ToString());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _taskService.UpdateTaskAsync(qaTask.Id, new UpdateTaskRequest { Title = "Hacked Title" }));
    }

    [Fact]
    public async Task User_UpdateTask_ThrowsForbidden()
    {
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        var dev1Task = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.AssignedToId == dev1!.Id);
        Assert.NotNull(dev1);
        Assert.NotNull(dev1Task);

        _currentUserService.SetUser(dev1.Id, dev1.Email!, UserRole.User.ToString());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _taskService.UpdateTaskAsync(dev1Task.Id, new UpdateTaskRequest { Title = "New Title" }));
    }

    [Fact]
    public async Task User_UpdateTaskStatus_ForOwnTask_Succeeds()
    {
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        var dev1Task = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.AssignedToId == dev1!.Id);
        Assert.NotNull(dev1);
        Assert.NotNull(dev1Task);

        _currentUserService.SetUser(dev1.Id, dev1.Email!, UserRole.User.ToString());

        var result = await _taskService.UpdateTaskStatusAsync(dev1Task.Id, new UpdateTaskStatusRequest
        {
            Status = TaskStatus.Done
        });

        Assert.NotNull(result);
        Assert.Equal(TaskStatus.Done.ToString(), result.Status);
    }

    [Fact]
    public async Task User_UpdateTaskStatus_ForOtherUserTask_ThrowsForbidden_IDOR()
    {
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        var dev2 = await _userManager.FindByEmailAsync("dev2@taskmanagement.com");
        var dev2Task = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.AssignedToId == dev2!.Id);
        Assert.NotNull(dev1);
        Assert.NotNull(dev2Task);

        _currentUserService.SetUser(dev1.Id, dev1.Email!, UserRole.User.ToString());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _taskService.UpdateTaskStatusAsync(dev2Task.Id, new UpdateTaskStatusRequest { Status = TaskStatus.Done }));
    }

    [Fact]
    public async Task Manager_AssignTask_WithinOwnTeam_Succeeds()
    {
        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        var dev2 = await _userManager.FindByEmailAsync("dev2@taskmanagement.com");
        var engTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "Engineering Team");
        var task = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.TeamId == engTeam!.Id && t.AssignedToId == dev1!.Id);
        Assert.NotNull(manager1);
        Assert.NotNull(dev2);
        Assert.NotNull(task);

        _currentUserService.SetUser(manager1.Id, manager1.Email!, UserRole.Manager.ToString());

        var result = await _taskService.AssignTaskAsync(task.Id, new AssignTaskRequest { AssignedToId = dev2.Id });

        Assert.NotNull(result);
        Assert.Equal(dev2.Id, result.AssignedToId);
        Assert.Equal(manager1.Id, result.AssignedById);
    }

    [Fact]
    public async Task AssignTask_ToUserFromAnotherTeam_ThrowsValidation()
    {
        var admin = await _userManager.FindByEmailAsync("admin@taskmanagement.com");
        var qa1 = await _userManager.FindByEmailAsync("qa1@taskmanagement.com");
        var engTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "Engineering Team");
        var task = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.TeamId == engTeam!.Id);
        Assert.NotNull(admin);
        Assert.NotNull(qa1);
        Assert.NotNull(task);

        _currentUserService.SetUser(admin.Id, admin.Email!, UserRole.Admin.ToString());

        await Assert.ThrowsAsync<ValidationException>(() =>
            _taskService.AssignTaskAsync(task.Id, new AssignTaskRequest { AssignedToId = qa1.Id }));
    }

    [Fact]
    public async Task Manager_DeleteTask_ForOwnTeam_Succeeds()
    {
        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        var engTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "Engineering Team");
        var task = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.TeamId == engTeam!.Id);
        Assert.NotNull(manager1);
        Assert.NotNull(task);

        _currentUserService.SetUser(manager1.Id, manager1.Email!, UserRole.Manager.ToString());

        await _taskService.DeleteTaskAsync(task.Id);

        var deletedTask = await _dbContext.Tasks.FindAsync(task.Id);
        Assert.Null(deletedTask);
    }

    [Fact]
    public async Task User_DeleteTask_ThrowsForbidden()
    {
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        var task = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.AssignedToId == dev1!.Id);
        Assert.NotNull(dev1);
        Assert.NotNull(task);

        _currentUserService.SetUser(dev1.Id, dev1.Email!, UserRole.User.ToString());

        await Assert.ThrowsAsync<ForbiddenException>(() => _taskService.DeleteTaskAsync(task.Id));
    }

    [Fact]
    public async Task GetTasks_FilteringByStatusAndPriority_Works()
    {
        var admin = await _userManager.FindByEmailAsync("admin@taskmanagement.com");
        Assert.NotNull(admin);

        _currentUserService.SetUser(admin.Id, admin.Email!, UserRole.Admin.ToString());

        var result = await _taskService.GetTasksAsync(new TaskQueryParameters
        {
            Status = "ToDo",
            Priority = "High"
        });

        Assert.NotNull(result);
        Assert.All(result.Items, t =>
        {
            Assert.Equal("ToDo", t.Status);
            Assert.Equal("High", t.Priority);
        });
    }

    [Fact]
    public async Task GetTasks_Pagination_Works()
    {
        var admin = await _userManager.FindByEmailAsync("admin@taskmanagement.com");
        Assert.NotNull(admin);

        _currentUserService.SetUser(admin.Id, admin.Email!, UserRole.Admin.ToString());

        var result = await _taskService.GetTasksAsync(new TaskQueryParameters { Page = 1, PageSize = 2 });

        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(2, result.Items.Count);
        Assert.True(result.TotalCount > 2);
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
