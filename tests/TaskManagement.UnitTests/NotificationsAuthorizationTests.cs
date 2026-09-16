using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Application.DTOs.Notifications;
using TaskManagement.Application.DTOs.Tasks;
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

public sealed class NotificationsAuthorizationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly NotificationRepository _notificationRepository;
    private readonly TaskRepository _taskRepository;
    private readonly TestCurrentUserService _currentUserService;
    private readonly NotificationService _notificationService;
    private readonly TaskService _taskService;

    public NotificationsAuthorizationTests()
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

        _notificationRepository = new NotificationRepository(_dbContext);
        _taskRepository = new TaskRepository(_dbContext);
        _currentUserService = new TestCurrentUserService();
        _notificationService = new NotificationService(_notificationRepository, _currentUserService);
        _taskService = new TaskService(_taskRepository, _currentUserService, _notificationService);

        DatabaseSeeder.SeedAsync(_serviceProvider).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task Admin_GetNotifications_ReturnsOnlyOwnNotifications()
    {
        var admin = await _userManager.FindByEmailAsync("admin@taskmanagement.com");
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        Assert.NotNull(admin);
        Assert.NotNull(dev1);

        // Add notifications for admin and dev1
        var n1 = new Notification { Id = Guid.NewGuid(), UserId = admin.Id, Message = "Admin alert", Type = NotificationType.Assignment, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var n2 = new Notification { Id = Guid.NewGuid(), UserId = dev1.Id, Message = "Dev alert", Type = NotificationType.Assignment, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _dbContext.Notifications.AddRange(n1, n2);
        await _dbContext.SaveChangesAsync();

        _currentUserService.SetUser(admin.Id, admin.Email!, UserRole.Admin.ToString());

        var result = await _notificationService.GetNotificationsAsync(new NotificationQueryParameters());

        Assert.NotNull(result);
        Assert.All(result.Items, item => Assert.Equal(admin.Id, item.UserId));
        Assert.Contains(result.Items, item => string.Equals(item.Message, "Admin alert", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Items, item => string.Equals(item.Message, "Dev alert", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Manager_GetNotifications_ReturnsOnlyOwnNotifications()
    {
        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        Assert.NotNull(manager1);
        Assert.NotNull(dev1);

        var n1 = new Notification { Id = Guid.NewGuid(), UserId = manager1.Id, Message = "Manager alert", Type = NotificationType.Assignment, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var n2 = new Notification { Id = Guid.NewGuid(), UserId = dev1.Id, Message = "Dev alert", Type = NotificationType.Assignment, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _dbContext.Notifications.AddRange(n1, n2);
        await _dbContext.SaveChangesAsync();

        _currentUserService.SetUser(manager1.Id, manager1.Email!, UserRole.Manager.ToString());

        var result = await _notificationService.GetNotificationsAsync(new NotificationQueryParameters());

        Assert.NotNull(result);
        Assert.All(result.Items, item => Assert.Equal(manager1.Id, item.UserId));
        Assert.Contains(result.Items, item => string.Equals(item.Message, "Manager alert", StringComparison.Ordinal));
    }

    [Fact]
    public async Task User_GetNotifications_WithIsReadFilter_Works()
    {
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        Assert.NotNull(dev1);

        var unread = new Notification { Id = Guid.NewGuid(), UserId = dev1.Id, Message = "Unread", IsRead = false, Type = NotificationType.Assignment, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var read = new Notification { Id = Guid.NewGuid(), UserId = dev1.Id, Message = "Read", IsRead = true, Type = NotificationType.StatusUpdate, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _dbContext.Notifications.AddRange(unread, read);
        await _dbContext.SaveChangesAsync();

        _currentUserService.SetUser(dev1.Id, dev1.Email!, UserRole.User.ToString());

        var unreadResult = await _notificationService.GetNotificationsAsync(new NotificationQueryParameters { IsRead = false });
        Assert.All(unreadResult.Items, item => Assert.False(item.IsRead));
        Assert.Contains(unreadResult.Items, item => string.Equals(item.Message, "Unread", StringComparison.Ordinal));
        Assert.DoesNotContain(unreadResult.Items, item => string.Equals(item.Message, "Read", StringComparison.Ordinal));

        var readResult = await _notificationService.GetNotificationsAsync(new NotificationQueryParameters { IsRead = true });
        Assert.All(readResult.Items, item => Assert.True(item.IsRead));
        Assert.Contains(readResult.Items, item => string.Equals(item.Message, "Read", StringComparison.Ordinal));
    }

    [Fact]
    public async Task User_GetNotifications_PaginationAndDeterministicOrder_Works()
    {
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        Assert.NotNull(dev1);

        var nOld = new Notification { Id = Guid.NewGuid(), UserId = dev1.Id, Message = "Older", CreatedAt = DateTime.UtcNow.AddMinutes(-10), UpdatedAt = DateTime.UtcNow.AddMinutes(-10) };
        var nNew = new Notification { Id = Guid.NewGuid(), UserId = dev1.Id, Message = "Newer", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _dbContext.Notifications.AddRange(nOld, nNew);
        await _dbContext.SaveChangesAsync();

        _currentUserService.SetUser(dev1.Id, dev1.Email!, UserRole.User.ToString());

        var result = await _notificationService.GetNotificationsAsync(new NotificationQueryParameters { Page = 1, PageSize = 10, SortDirection = "desc" });

        Assert.True(result.Items.Count >= 2);
        var idxNew = result.Items.ToList().FindIndex(n => string.Equals(n.Message, "Newer", StringComparison.Ordinal));
        var idxOld = result.Items.ToList().FindIndex(n => string.Equals(n.Message, "Older", StringComparison.Ordinal));
        Assert.True(idxNew < idxOld);
    }

    [Fact]
    public async Task User_MarkAsRead_OwnNotification_Succeeds()
    {
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        Assert.NotNull(dev1);

        var n = new Notification { Id = Guid.NewGuid(), UserId = dev1.Id, Message = "To Read", IsRead = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _dbContext.Notifications.Add(n);
        await _dbContext.SaveChangesAsync();

        _currentUserService.SetUser(dev1.Id, dev1.Email!, UserRole.User.ToString());

        var updated = await _notificationService.MarkAsReadAsync(n.Id);

        Assert.True(updated.IsRead);
        var inDb = await _dbContext.Notifications.FindAsync(n.Id);
        Assert.NotNull(inDb);
        Assert.True(inDb.IsRead);
    }

    [Fact]
    public async Task User_MarkAsRead_OtherUserNotification_ThrowsForbidden_IDOR()
    {
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        var dev2 = await _userManager.FindByEmailAsync("dev2@taskmanagement.com");
        Assert.NotNull(dev1);
        Assert.NotNull(dev2);

        var n = new Notification { Id = Guid.NewGuid(), UserId = dev2.Id, Message = "Dev2 Private", IsRead = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _dbContext.Notifications.Add(n);
        await _dbContext.SaveChangesAsync();

        _currentUserService.SetUser(dev1.Id, dev1.Email!, UserRole.User.ToString());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _notificationService.MarkAsReadAsync(n.Id));
    }

    [Fact]
    public async Task User_MarkAllAsRead_OnlyAffectsOwnNotifications()
    {
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        var dev2 = await _userManager.FindByEmailAsync("dev2@taskmanagement.com");
        Assert.NotNull(dev1);
        Assert.NotNull(dev2);

        var n1 = new Notification { Id = Guid.NewGuid(), UserId = dev1.Id, Message = "Dev1 Msg 1", IsRead = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var n2 = new Notification { Id = Guid.NewGuid(), UserId = dev1.Id, Message = "Dev1 Msg 2", IsRead = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var n3 = new Notification { Id = Guid.NewGuid(), UserId = dev2.Id, Message = "Dev2 Msg", IsRead = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _dbContext.Notifications.AddRange(n1, n2, n3);
        await _dbContext.SaveChangesAsync();

        _currentUserService.SetUser(dev1.Id, dev1.Email!, UserRole.User.ToString());

        await _notificationService.MarkAllAsReadAsync();

        var dev1_1 = await _dbContext.Notifications.FindAsync(n1.Id);
        var dev1_2 = await _dbContext.Notifications.FindAsync(n2.Id);
        var dev2_1 = await _dbContext.Notifications.FindAsync(n3.Id);

        Assert.True(dev1_1!.IsRead);
        Assert.True(dev1_2!.IsRead);
        Assert.False(dev2_1!.IsRead);
    }

    [Fact]
    public async Task TaskAssignment_CreatesNotificationForAssignee()
    {
        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        var dev2 = await _userManager.FindByEmailAsync("dev2@taskmanagement.com");
        var engTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "Engineering Team");
        Assert.NotNull(manager1);
        Assert.NotNull(dev1);
        Assert.NotNull(dev2);
        Assert.NotNull(engTeam);

        _currentUserService.SetUser(manager1.Id, manager1.Email!, UserRole.Manager.ToString());

        var task = await _taskService.CreateTaskAsync(new CreateTaskRequest
        {
            Title = "Assignment Notification Test Task",
            Deadline = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            TeamId = engTeam.Id,
            AssignedToId = dev1.Id
        });

        // Verify notification for dev1
        var dev1Notifs = await _dbContext.Notifications.Where(n => n.UserId == dev1.Id && n.TaskId == task.Id).ToListAsync();
        Assert.Single(dev1Notifs);
        Assert.Equal(NotificationType.Assignment, dev1Notifs[0].Type);
        Assert.Contains("Assignment Notification Test Task", dev1Notifs[0].Message, StringComparison.Ordinal);

        // Now reassign to dev2
        await _taskService.AssignTaskAsync(task.Id, new AssignTaskRequest { AssignedToId = dev2.Id });

        var dev2Notifs = await _dbContext.Notifications.Where(n => n.UserId == dev2.Id && n.TaskId == task.Id).ToListAsync();
        Assert.Single(dev2Notifs);
        Assert.Equal(NotificationType.Assignment, dev2Notifs[0].Type);
    }

    [Fact]
    public async Task TaskStatusUpdate_WhenChanged_CreatesStatusUpdateNotification()
    {
        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        var engTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "Engineering Team");
        Assert.NotNull(manager1);
        Assert.NotNull(dev1);
        Assert.NotNull(engTeam);

        _currentUserService.SetUser(manager1.Id, manager1.Email!, UserRole.Manager.ToString());

        var task = await _taskService.CreateTaskAsync(new CreateTaskRequest
        {
            Title = "Status Notification Test Task",
            Status = TaskStatus.ToDo,
            Deadline = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            TeamId = engTeam.Id,
            AssignedToId = dev1.Id
        });

        // Dev1 updates status to InProgress
        _currentUserService.SetUser(dev1.Id, dev1.Email!, UserRole.User.ToString());
        await _taskService.UpdateTaskStatusAsync(task.Id, new UpdateTaskStatusRequest { Status = TaskStatus.InProgress });

        var notifsAfterChange = await _dbContext.Notifications.Where(n => n.TaskId == task.Id && n.Type == NotificationType.StatusUpdate).ToListAsync();
        Assert.Single(notifsAfterChange);
        Assert.Equal(dev1.Id, notifsAfterChange[0].UserId);
        Assert.Contains("Status Notification Test Task", notifsAfterChange[0].Message, StringComparison.Ordinal);

        // Dev1 updates status with SAME status (InProgress -> InProgress)
        await _taskService.UpdateTaskStatusAsync(task.Id, new UpdateTaskStatusRequest { Status = TaskStatus.InProgress });
        var notifsAfterSame = await _dbContext.Notifications.Where(n => n.TaskId == task.Id && n.Type == NotificationType.StatusUpdate).ToListAsync();
        Assert.Single(notifsAfterSame); // count should NOT increase
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
