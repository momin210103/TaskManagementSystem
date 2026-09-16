using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using Xunit;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.UnitTests;

public sealed class TaskServiceTests
{
    private readonly TestTaskRepository _taskRepository = new();
    private readonly TestCurrentUserService _currentUserService = new();
    private readonly TestNotificationService _notificationService = new();
    private readonly TaskService _taskService;

    public TaskServiceTests()
    {
        _taskService = new TaskService(_taskRepository, _currentUserService, _notificationService);
    }

    [Fact]
    public async Task CreateTaskAsync_AsAdmin_WithValidData_Succeeds()
    {
        var adminId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();

        _currentUserService.SetUser(adminId, "admin@test.com", UserRole.Admin.ToString());
        _taskRepository.ExistingTeams.Add(teamId);
        _taskRepository.ExistingUsers.Add(assigneeId);
        _taskRepository.UserTeamMap[assigneeId] = teamId;

        var request = new CreateTaskRequest
        {
            Title = "Setup CI pipeline",
            Description = "Configure GitHub actions",
            Status = TaskStatus.ToDo,
            Priority = TaskPriority.High,
            Deadline = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            TeamId = teamId,
            AssignedToId = assigneeId
        };

        var result = await _taskService.CreateTaskAsync(request);

        Assert.NotNull(result);
        Assert.Equal("Setup CI pipeline", result.Title);
        Assert.Equal(adminId, result.AssignedById);
        Assert.Equal(teamId, result.TeamId);
        Assert.Equal(assigneeId, result.AssignedToId);
    }

    [Fact]
    public async Task CreateTaskAsync_AsManager_ForOwnTeam_Succeeds()
    {
        var managerId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();

        _currentUserService.SetUser(managerId, "manager@test.com", UserRole.Manager.ToString());
        _taskRepository.ExistingTeams.Add(teamId);
        _taskRepository.ExistingUsers.Add(assigneeId);
        _taskRepository.UserTeamMap[assigneeId] = teamId;
        _taskRepository.ManagerTeamMap[managerId] = teamId;
        _taskRepository.TeamManagerMap[teamId] = managerId;

        var request = new CreateTaskRequest
        {
            Title = "Manager Task",
            Deadline = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            TeamId = teamId,
            AssignedToId = assigneeId
        };

        var result = await _taskService.CreateTaskAsync(request);

        Assert.NotNull(result);
        Assert.Equal(managerId, result.AssignedById);
    }

    [Fact]
    public async Task CreateTaskAsync_AsManager_ForOtherTeam_ThrowsForbidden()
    {
        var managerId = Guid.NewGuid();
        var ownTeamId = Guid.NewGuid();
        var otherTeamId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();

        _currentUserService.SetUser(managerId, "manager@test.com", UserRole.Manager.ToString());
        _taskRepository.ManagerTeamMap[managerId] = ownTeamId;

        var request = new CreateTaskRequest
        {
            Title = "Other Team Task",
            Deadline = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            TeamId = otherTeamId,
            AssignedToId = assigneeId
        };

        await Assert.ThrowsAsync<ForbiddenException>(() => _taskService.CreateTaskAsync(request));
    }

    [Fact]
    public async Task CreateTaskAsync_AsUser_ThrowsForbidden()
    {
        var userId = Guid.NewGuid();
        _currentUserService.SetUser(userId, "user@test.com", UserRole.User.ToString());

        var request = new CreateTaskRequest
        {
            Title = "User Task",
            Deadline = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            TeamId = Guid.NewGuid(),
            AssignedToId = userId
        };

        await Assert.ThrowsAsync<ForbiddenException>(() => _taskService.CreateTaskAsync(request));
    }

    [Fact]
    public async Task CreateTaskAsync_WithAssigneeFromDifferentTeam_ThrowsValidation()
    {
        var adminId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var otherTeamId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();

        _currentUserService.SetUser(adminId, "admin@test.com", UserRole.Admin.ToString());
        _taskRepository.ExistingTeams.Add(teamId);
        _taskRepository.ExistingUsers.Add(assigneeId);
        _taskRepository.UserTeamMap[assigneeId] = otherTeamId;

        var request = new CreateTaskRequest
        {
            Title = "Cross Team Task",
            Deadline = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            TeamId = teamId,
            AssignedToId = assigneeId
        };

        await Assert.ThrowsAsync<ValidationException>(() => _taskService.CreateTaskAsync(request));
    }

    [Fact]
    public async Task CreateTaskAsync_WithEmptyTitle_ThrowsValidation()
    {
        var adminId = Guid.NewGuid();
        _currentUserService.SetUser(adminId, "admin@test.com", UserRole.Admin.ToString());

        var request = new CreateTaskRequest
        {
            Title = "   ",
            Deadline = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            TeamId = Guid.NewGuid(),
            AssignedToId = Guid.NewGuid()
        };

        await Assert.ThrowsAsync<ValidationException>(() => _taskService.CreateTaskAsync(request));
    }

    [Fact]
    public async Task GetTasksAsync_AsAdmin_ReturnsAllTasks()
    {
        var adminId = Guid.NewGuid();
        _currentUserService.SetUser(adminId, "admin@test.com", UserRole.Admin.ToString());

        _taskRepository.PagedResultToReturn = new PagedResult<TaskResponse>
        {
            Items = [new TaskResponse { Id = Guid.NewGuid(), Title = "Task 1" }, new TaskResponse { Id = Guid.NewGuid(), Title = "Task 2" }],
            TotalCount = 2
        };

        var result = await _taskService.GetTasksAsync(new TaskQueryParameters());

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetTasksAsync_AsManager_WithOtherTeamFilter_ThrowsForbidden()
    {
        var managerId = Guid.NewGuid();
        var ownTeamId = Guid.NewGuid();
        var otherTeamId = Guid.NewGuid();

        _currentUserService.SetUser(managerId, "manager@test.com", UserRole.Manager.ToString());
        _taskRepository.ManagerTeamMap[managerId] = ownTeamId;

        var parameters = new TaskQueryParameters { TeamId = otherTeamId };

        await Assert.ThrowsAsync<ForbiddenException>(() => _taskService.GetTasksAsync(parameters));
    }

    [Fact]
    public async Task GetTaskByIdAsync_AsUser_ForOtherUserTask_ThrowsForbidden_IDOR()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        _currentUserService.SetUser(userId, "user@test.com", UserRole.User.ToString());

        var task = new TaskItem
        {
            Id = taskId,
            Title = "Secret Task",
            AssignedToId = otherUserId,
            TeamId = Guid.NewGuid()
        };
        _taskRepository.TasksMap[taskId] = task;

        await Assert.ThrowsAsync<ForbiddenException>(() => _taskService.GetTaskByIdAsync(taskId));
    }

    [Fact]
    public async Task GetTaskByIdAsync_AsManager_ForOtherTeamTask_ThrowsForbidden_IDOR()
    {
        var managerId = Guid.NewGuid();
        var ownTeamId = Guid.NewGuid();
        var otherTeamId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        _currentUserService.SetUser(managerId, "manager@test.com", UserRole.Manager.ToString());
        _taskRepository.ManagerTeamMap[managerId] = ownTeamId;
        _taskRepository.TeamManagerMap[otherTeamId] = Guid.NewGuid();

        var task = new TaskItem
        {
            Id = taskId,
            Title = "Other Team Task",
            TeamId = otherTeamId,
            AssignedToId = Guid.NewGuid()
        };
        _taskRepository.TasksMap[taskId] = task;

        await Assert.ThrowsAsync<ForbiddenException>(() => _taskService.GetTaskByIdAsync(taskId));
    }

    [Fact]
    public async Task UpdateTaskAsync_AsAdmin_Succeeds()
    {
        var adminId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        _currentUserService.SetUser(adminId, "admin@test.com", UserRole.Admin.ToString());

        var task = new TaskItem
        {
            Id = taskId,
            Title = "Old Title",
            Priority = TaskPriority.Low,
            TeamId = Guid.NewGuid()
        };
        _taskRepository.TasksMap[taskId] = task;

        var request = new UpdateTaskRequest
        {
            Title = "New Title",
            Priority = TaskPriority.High
        };

        var result = await _taskService.UpdateTaskAsync(taskId, request);

        Assert.NotNull(result);
        Assert.Equal("New Title", task.Title);
        Assert.Equal(TaskPriority.High, task.Priority);
    }

    [Fact]
    public async Task UpdateTaskAsync_AsUser_ThrowsForbidden()
    {
        var userId = Guid.NewGuid();
        _currentUserService.SetUser(userId, "user@test.com", UserRole.User.ToString());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _taskService.UpdateTaskAsync(Guid.NewGuid(), new UpdateTaskRequest { Title = "New Title" }));
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_AsAssignee_Succeeds()
    {
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        _currentUserService.SetUser(userId, "user@test.com", UserRole.User.ToString());

        var task = new TaskItem
        {
            Id = taskId,
            Title = "My Task",
            Status = TaskStatus.ToDo,
            AssignedToId = userId,
            TeamId = Guid.NewGuid()
        };
        _taskRepository.TasksMap[taskId] = task;

        var result = await _taskService.UpdateTaskStatusAsync(taskId, new UpdateTaskStatusRequest { Status = TaskStatus.InProgress });

        Assert.NotNull(result);
        Assert.Equal(TaskStatus.InProgress, task.Status);
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_AsUser_ForOtherUserTask_ThrowsForbidden_IDOR()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        _currentUserService.SetUser(userId, "user@test.com", UserRole.User.ToString());

        var task = new TaskItem
        {
            Id = taskId,
            Title = "Other User Task",
            Status = TaskStatus.ToDo,
            AssignedToId = otherUserId,
            TeamId = Guid.NewGuid()
        };
        _taskRepository.TasksMap[taskId] = task;

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _taskService.UpdateTaskStatusAsync(taskId, new UpdateTaskStatusRequest { Status = TaskStatus.Done }));
    }

    [Fact]
    public async Task AssignTaskAsync_AsManager_WithinOwnTeam_Succeeds()
    {
        var managerId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var originalAssigneeId = Guid.NewGuid();
        var newAssigneeId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        _currentUserService.SetUser(managerId, "manager@test.com", UserRole.Manager.ToString());
        _taskRepository.TeamManagerMap[teamId] = managerId;
        _taskRepository.ExistingUsers.Add(newAssigneeId);
        _taskRepository.UserTeamMap[newAssigneeId] = teamId;

        var task = new TaskItem
        {
            Id = taskId,
            Title = "Team Task",
            TeamId = teamId,
            AssignedToId = originalAssigneeId
        };
        _taskRepository.TasksMap[taskId] = task;

        var result = await _taskService.AssignTaskAsync(taskId, new AssignTaskRequest { AssignedToId = newAssigneeId });

        Assert.NotNull(result);
        Assert.Equal(newAssigneeId, task.AssignedToId);
        Assert.Equal(managerId, task.AssignedById);
    }

    [Fact]
    public async Task AssignTaskAsync_ToUserFromAnotherTeam_ThrowsValidation()
    {
        var adminId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var otherTeamId = Guid.NewGuid();
        var newAssigneeId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        _currentUserService.SetUser(adminId, "admin@test.com", UserRole.Admin.ToString());
        _taskRepository.ExistingUsers.Add(newAssigneeId);
        _taskRepository.UserTeamMap[newAssigneeId] = otherTeamId;

        var task = new TaskItem
        {
            Id = taskId,
            Title = "Team Task",
            TeamId = teamId,
            AssignedToId = Guid.NewGuid()
        };
        _taskRepository.TasksMap[taskId] = task;

        await Assert.ThrowsAsync<ValidationException>(() =>
            _taskService.AssignTaskAsync(taskId, new AssignTaskRequest { AssignedToId = newAssigneeId }));
    }

    [Fact]
    public async Task DeleteTaskAsync_AsManager_ForOwnTeam_Succeeds()
    {
        var managerId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        _currentUserService.SetUser(managerId, "manager@test.com", UserRole.Manager.ToString());
        _taskRepository.TeamManagerMap[teamId] = managerId;

        var task = new TaskItem
        {
            Id = taskId,
            Title = "Team Task",
            TeamId = teamId
        };
        _taskRepository.TasksMap[taskId] = task;

        await _taskService.DeleteTaskAsync(taskId);

        Assert.False(_taskRepository.TasksMap.ContainsKey(taskId));
    }

    [Fact]
    public async Task DeleteTaskAsync_AsUser_ThrowsForbidden()
    {
        var userId = Guid.NewGuid();
        _currentUserService.SetUser(userId, "user@test.com", UserRole.User.ToString());

        await Assert.ThrowsAsync<ForbiddenException>(() => _taskService.DeleteTaskAsync(Guid.NewGuid()));
    }

    private sealed class TestTaskRepository : ITaskRepository
    {
        public Dictionary<Guid, TaskItem> TasksMap { get; } = new();
        public HashSet<Guid> ExistingTeams { get; } = [];
        public HashSet<Guid> ExistingUsers { get; } = [];
        public Dictionary<Guid, Guid> UserTeamMap { get; } = new();
        public Dictionary<Guid, Guid> ManagerTeamMap { get; } = new();
        public Dictionary<Guid, Guid> TeamManagerMap { get; } = new();
        public PagedResult<TaskResponse>? PagedResultToReturn { get; set; }

        public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            TasksMap.TryGetValue(id, out var task);
            return Task.FromResult(task);
        }

        public Task<TaskResponse?> GetResponseByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (TasksMap.TryGetValue(id, out var task))
            {
                return Task.FromResult<TaskResponse?>(new TaskResponse
                {
                    Id = task.Id,
                    Title = task.Title,
                    Description = task.Description,
                    Status = task.Status.ToString(),
                    Priority = task.Priority.ToString(),
                    Deadline = task.Deadline,
                    TeamId = task.TeamId,
                    AssignedToId = task.AssignedToId,
                    AssignedById = task.AssignedById,
                    CreatedAt = task.CreatedAt,
                    UpdatedAt = task.UpdatedAt
                });
            }
            return Task.FromResult<TaskResponse?>(null);
        }

        public Task<PagedResult<TaskResponse>> GetPagedAsync(
            TaskQueryParameters parameters,
            Guid? enforcedTeamId = null,
            Guid? enforcedAssignedToId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PagedResultToReturn ?? new PagedResult<TaskResponse>());
        }

        public Task<TaskItem> CreateAsync(TaskItem task, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(task);
            TasksMap[task.Id] = task;
            return Task.FromResult(task);
        }

        public Task<bool> UpdateAsync(TaskItem task, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(task);
            TasksMap[task.Id] = task;
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(TasksMap.Remove(id));
        }

        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(TasksMap.ContainsKey(id));
        }

        public Task<bool> TeamExistsAsync(Guid teamId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ExistingTeams.Contains(teamId));
        }

        public Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ExistingUsers.Contains(userId));
        }

        public Task<Guid?> GetUserTeamIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            if (UserTeamMap.TryGetValue(userId, out var teamId))
            {
                return Task.FromResult<Guid?>(teamId);
            }
            return Task.FromResult<Guid?>(null);
        }

        public Task<Guid?> GetTeamManagerIdAsync(Guid teamId, CancellationToken cancellationToken = default)
        {
            if (TeamManagerMap.TryGetValue(teamId, out var managerId))
            {
                return Task.FromResult<Guid?>(managerId);
            }
            return Task.FromResult<Guid?>(null);
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

    [Fact]
    public async Task CreateTaskAsync_CreatesAssignmentNotificationForAssignee()
    {
        var adminId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();

        _currentUserService.SetUser(adminId, "admin@test.com", UserRole.Admin.ToString());
        _taskRepository.ExistingTeams.Add(teamId);
        _taskRepository.ExistingUsers.Add(assigneeId);
        _taskRepository.UserTeamMap[assigneeId] = teamId;

        var request = new CreateTaskRequest
        {
            Title = "Task with Notification",
            Deadline = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            TeamId = teamId,
            AssignedToId = assigneeId
        };

        var result = await _taskService.CreateTaskAsync(request);

        Assert.NotNull(result);
        Assert.Single(_notificationService.CreatedNotifications);
        var notification = _notificationService.CreatedNotifications[0];
        Assert.Equal(assigneeId, notification.UserId);
        Assert.Equal(NotificationType.Assignment, notification.Type);
        Assert.Contains("Task with Notification", notification.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AssignTaskAsync_CreatesAssignmentNotificationForNewAssignee()
    {
        var managerId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var initialAssigneeId = Guid.NewGuid();
        var newAssigneeId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        _currentUserService.SetUser(managerId, "manager@test.com", UserRole.Manager.ToString());
        _taskRepository.TeamManagerMap[teamId] = managerId;
        _taskRepository.ExistingUsers.Add(newAssigneeId);
        _taskRepository.UserTeamMap[newAssigneeId] = teamId;

        var task = new TaskItem
        {
            Id = taskId,
            Title = "Reassigned Task",
            TeamId = teamId,
            AssignedToId = initialAssigneeId
        };
        _taskRepository.TasksMap[taskId] = task;

        await _taskService.AssignTaskAsync(taskId, new AssignTaskRequest { AssignedToId = newAssigneeId });

        Assert.Single(_notificationService.CreatedNotifications);
        var notification = _notificationService.CreatedNotifications[0];
        Assert.Equal(newAssigneeId, notification.UserId);
        Assert.Equal(NotificationType.Assignment, notification.Type);
        Assert.Contains("Reassigned Task", notification.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_WhenStatusChanges_CreatesStatusNotification()
    {
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        _currentUserService.SetUser(userId, "user@test.com", UserRole.User.ToString());

        var task = new TaskItem
        {
            Id = taskId,
            Title = "Status Update Task",
            Status = TaskStatus.ToDo,
            AssignedToId = userId,
            TeamId = Guid.NewGuid()
        };
        _taskRepository.TasksMap[taskId] = task;

        await _taskService.UpdateTaskStatusAsync(taskId, new UpdateTaskStatusRequest { Status = TaskStatus.InProgress });

        Assert.Single(_notificationService.CreatedNotifications);
        var notification = _notificationService.CreatedNotifications[0];
        Assert.Equal(userId, notification.UserId);
        Assert.Equal(NotificationType.StatusUpdate, notification.Type);
        Assert.Contains("Status Update Task", notification.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_WhenStatusUnchanged_DoesNotCreateNotification()
    {
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        _currentUserService.SetUser(userId, "user@test.com", UserRole.User.ToString());

        var task = new TaskItem
        {
            Id = taskId,
            Title = "Same Status Task",
            Status = TaskStatus.InProgress,
            AssignedToId = userId,
            TeamId = Guid.NewGuid()
        };
        _taskRepository.TasksMap[taskId] = task;

        await _taskService.UpdateTaskStatusAsync(taskId, new UpdateTaskStatusRequest { Status = TaskStatus.InProgress });

        Assert.Empty(_notificationService.CreatedNotifications);
    }

    private sealed class TestNotificationService : INotificationService
    {
        public List<(Guid UserId, Guid? TaskId, NotificationType Type, string Message)> CreatedNotifications { get; } = [];

        public Task<PagedResult<TaskManagement.Application.DTOs.Notifications.NotificationResponse>> GetNotificationsAsync(
            TaskManagement.Application.DTOs.Notifications.NotificationQueryParameters parameters,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PagedResult<TaskManagement.Application.DTOs.Notifications.NotificationResponse>());
        }

        public Task<TaskManagement.Application.DTOs.Notifications.NotificationResponse> MarkAsReadAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new TaskManagement.Application.DTOs.Notifications.NotificationResponse { Id = id, IsRead = true });
        }

        public Task MarkAllAsReadAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task CreateNotificationAsync(
            Guid userId,
            Guid? taskId,
            NotificationType type,
            string message,
            CancellationToken cancellationToken = default)
        {
            CreatedNotifications.Add((userId, taskId, type, message));
            return Task.CompletedTask;
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

        public bool IsInRole(string role)
        {
            return string.Equals(Role, role, StringComparison.OrdinalIgnoreCase);
        }
    }
}


