using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Application.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly INotificationService _notificationService;

    public TaskService(
        ITaskRepository taskRepository,
        ICurrentUserService currentUserService,
        INotificationService notificationService)
    {
        ArgumentNullException.ThrowIfNull(taskRepository);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(notificationService);

        _taskRepository = taskRepository;
        _currentUserService = currentUserService;
        _notificationService = notificationService;
    }

    public async Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var currentUserId = GetCurrentUserIdOrThrow();

        if (_currentUserService.IsInRole(UserRole.User.ToString()))
        {
            throw new ForbiddenException("Users cannot create tasks.");
        }

        if (_currentUserService.IsInRole(UserRole.Manager.ToString()))
        {
            await AuthorizeManagerTeamScopeAsync(currentUserId, request.TeamId, cancellationToken).ConfigureAwait(false);
        }

        ValidateCreateTaskRequest(request);
        await ValidateTeamAndAssigneeAsync(request.TeamId, request.AssignedToId, cancellationToken).ConfigureAwait(false);

        var now = DateTime.UtcNow;
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Status = request.Status,
            Priority = request.Priority,
            Deadline = request.Deadline,
            TeamId = request.TeamId,
            AssignedToId = request.AssignedToId,
            AssignedById = currentUserId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _taskRepository.CreateAsync(task, cancellationToken).ConfigureAwait(false);

        if (task.AssignedToId != Guid.Empty)
        {
            await _notificationService.CreateNotificationAsync(
                task.AssignedToId,
                task.Id,
                NotificationType.Assignment,
                $"You have been assigned a new task: {task.Title}",
                cancellationToken).ConfigureAwait(false);
        }

        return await _taskRepository.GetResponseByIdAsync(task.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Task with ID '{task.Id}' was not found.");
    }

    public async Task<PagedResult<TaskResponse>> GetTasksAsync(TaskQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var currentUserId = GetCurrentUserIdOrThrow();

        if (_currentUserService.IsInRole(UserRole.Admin.ToString()))
        {
            return await _taskRepository.GetPagedAsync(parameters, null, null, cancellationToken).ConfigureAwait(false);
        }

        if (_currentUserService.IsInRole(UserRole.Manager.ToString()))
        {
            var managedTeamId = await _taskRepository.GetManagedTeamIdAsync(currentUserId, cancellationToken).ConfigureAwait(false);
            if (!managedTeamId.HasValue)
            {
                return new PagedResult<TaskResponse> { Items = [], Page = parameters.Page, PageSize = parameters.PageSize, TotalCount = 0 };
            }

            if (parameters.TeamId.HasValue && parameters.TeamId.Value != managedTeamId.Value)
            {
                throw new ForbiddenException("Managers can only view tasks belonging to their own team.");
            }

            return await _taskRepository.GetPagedAsync(parameters, managedTeamId.Value, null, cancellationToken).ConfigureAwait(false);
        }

        // Normal User: strictly scoped to own assigned tasks (ignores arbitrary parameters.AssignedToId)
        return await _taskRepository.GetPagedAsync(parameters, null, currentUserId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TaskResponse> GetTaskByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserIdOrThrow();
        var task = await _taskRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

        if (task is null)
        {
            throw new NotFoundException($"Task with ID '{id}' was not found.");
        }

        await AuthorizeTaskAccessAsync(task, currentUserId, cancellationToken).ConfigureAwait(false);

        return await _taskRepository.GetResponseByIdAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Task with ID '{id}' was not found.");
    }

    public async Task<TaskResponse> UpdateTaskAsync(Guid id, UpdateTaskRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var currentUserId = GetCurrentUserIdOrThrow();

        if (_currentUserService.IsInRole(UserRole.User.ToString()))
        {
            throw new ForbiddenException("Users cannot update general task details.");
        }

        var task = await _taskRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (task is null)
        {
            throw new NotFoundException($"Task with ID '{id}' was not found.");
        }

        if (_currentUserService.IsInRole(UserRole.Manager.ToString()))
        {
            await AuthorizeManagerTeamOwnershipAsync(currentUserId, task.TeamId, cancellationToken).ConfigureAwait(false);
        }

        ApplyTaskUpdates(task, request);
        task.UpdatedAt = DateTime.UtcNow;

        await _taskRepository.UpdateAsync(task, cancellationToken).ConfigureAwait(false);

        return await _taskRepository.GetResponseByIdAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Task with ID '{id}' was not found.");
    }

    public async Task<TaskResponse> UpdateTaskStatusAsync(Guid id, UpdateTaskStatusRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var currentUserId = GetCurrentUserIdOrThrow();
        var task = await _taskRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

        if (task is null)
        {
            throw new NotFoundException($"Task with ID '{id}' was not found.");
        }

        if (request.Status == TaskStatus.None || !Enum.IsDefined(request.Status))
        {
            throw new ValidationException("Invalid task status.");
        }

        await AuthorizeStatusUpdateAsync(task, currentUserId, cancellationToken).ConfigureAwait(false);

        var oldStatus = task.Status;
        if (oldStatus != request.Status)
        {
            task.Status = request.Status;
            task.UpdatedAt = DateTime.UtcNow;

            await _taskRepository.UpdateAsync(task, cancellationToken).ConfigureAwait(false);

            if (task.AssignedToId != Guid.Empty)
            {
                await _notificationService.CreateNotificationAsync(
                    task.AssignedToId,
                    task.Id,
                    NotificationType.StatusUpdate,
                    $"You have a status update for task: {task.Title}",
                    cancellationToken).ConfigureAwait(false);
            }
        }

        return await _taskRepository.GetResponseByIdAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Task with ID '{id}' was not found.");
    }

    public async Task<TaskResponse> AssignTaskAsync(Guid id, AssignTaskRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var currentUserId = GetCurrentUserIdOrThrow();

        if (_currentUserService.IsInRole(UserRole.User.ToString()))
        {
            throw new ForbiddenException("Users cannot assign tasks.");
        }

        var task = await _taskRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (task is null)
        {
            throw new NotFoundException($"Task with ID '{id}' was not found.");
        }

        if (_currentUserService.IsInRole(UserRole.Manager.ToString()))
        {
            await AuthorizeManagerTeamOwnershipAsync(currentUserId, task.TeamId, cancellationToken).ConfigureAwait(false);
        }

        await ValidateAssigneeMembershipAsync(task.TeamId, request.AssignedToId, cancellationToken).ConfigureAwait(false);

        task.AssignedToId = request.AssignedToId;
        task.AssignedById = currentUserId;
        task.UpdatedAt = DateTime.UtcNow;

        await _taskRepository.UpdateAsync(task, cancellationToken).ConfigureAwait(false);

        await _notificationService.CreateNotificationAsync(
            request.AssignedToId,
            task.Id,
            NotificationType.Assignment,
            $"You have been assigned a new task: {task.Title}",
            cancellationToken).ConfigureAwait(false);

        return await _taskRepository.GetResponseByIdAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Task with ID '{id}' was not found.");
    }

    public async Task DeleteTaskAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserIdOrThrow();

        if (_currentUserService.IsInRole(UserRole.User.ToString()))
        {
            throw new ForbiddenException("Users cannot delete tasks.");
        }

        var task = await _taskRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (task is null)
        {
            throw new NotFoundException($"Task with ID '{id}' was not found.");
        }

        if (_currentUserService.IsInRole(UserRole.Manager.ToString()))
        {
            await AuthorizeManagerTeamOwnershipAsync(currentUserId, task.TeamId, cancellationToken).ConfigureAwait(false);
        }

        await _taskRepository.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
    }

    private Guid GetCurrentUserIdOrThrow()
    {
        return _currentUserService.UserId ?? throw new AuthException("User is not authenticated.");
    }

    private static void ValidateCreateTaskRequest(CreateTaskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ValidationException("Task title is required.");
        }

        if (request.Title.Trim().Length > 150)
        {
            throw new ValidationException("Task title cannot exceed 150 characters.");
        }

        if (request.Status == TaskStatus.None || !Enum.IsDefined(request.Status))
        {
            throw new ValidationException("Invalid task status.");
        }

        if (request.Priority == TaskPriority.None || !Enum.IsDefined(request.Priority))
        {
            throw new ValidationException("Invalid task priority.");
        }
    }

    private static void ApplyTaskUpdates(TaskItem task, UpdateTaskRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            if (request.Title.Trim().Length > 150)
            {
                throw new ValidationException("Task title cannot exceed 150 characters.");
            }
            task.Title = request.Title.Trim();
        }

        if (request.Description is not null)
        {
            task.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        }

        if (request.Priority.HasValue)
        {
            if (request.Priority.Value == TaskPriority.None || !Enum.IsDefined(request.Priority.Value))
            {
                throw new ValidationException("Invalid task priority.");
            }
            task.Priority = request.Priority.Value;
        }

        if (request.Deadline.HasValue)
        {
            task.Deadline = request.Deadline.Value;
        }
    }

    private async Task AuthorizeManagerTeamScopeAsync(Guid currentUserId, Guid teamId, CancellationToken cancellationToken)
    {
        var managedTeamId = await _taskRepository.GetManagedTeamIdAsync(currentUserId, cancellationToken).ConfigureAwait(false);
        if (!managedTeamId.HasValue || managedTeamId.Value != teamId)
        {
            throw new ForbiddenException("Managers can only create tasks for their own managed team.");
        }
    }

    private async Task AuthorizeManagerTeamOwnershipAsync(Guid currentUserId, Guid teamId, CancellationToken cancellationToken)
    {
        var teamManagerId = await _taskRepository.GetTeamManagerIdAsync(teamId, cancellationToken).ConfigureAwait(false);
        if (!teamManagerId.HasValue || teamManagerId.Value != currentUserId)
        {
            throw new ForbiddenException("Managers can only manage tasks belonging to their own team.");
        }
    }

    private async Task AuthorizeTaskAccessAsync(TaskItem task, Guid currentUserId, CancellationToken cancellationToken)
    {
        if (_currentUserService.IsInRole(UserRole.Admin.ToString()))
        {
            return;
        }

        if (_currentUserService.IsInRole(UserRole.Manager.ToString()))
        {
            var teamManagerId = await _taskRepository.GetTeamManagerIdAsync(task.TeamId, cancellationToken).ConfigureAwait(false);
            if (teamManagerId == currentUserId)
            {
                return;
            }

            throw new ForbiddenException("Managers can only view tasks belonging to their own team.");
        }

        if (task.AssignedToId == currentUserId)
        {
            return;
        }

        throw new ForbiddenException("Users can only view tasks assigned to themselves.");
    }

    private async Task AuthorizeStatusUpdateAsync(TaskItem task, Guid currentUserId, CancellationToken cancellationToken)
    {
        if (_currentUserService.IsInRole(UserRole.Admin.ToString()))
        {
            return;
        }

        if (_currentUserService.IsInRole(UserRole.Manager.ToString()))
        {
            var teamManagerId = await _taskRepository.GetTeamManagerIdAsync(task.TeamId, cancellationToken).ConfigureAwait(false);
            if (teamManagerId == currentUserId)
            {
                return;
            }

            throw new ForbiddenException("Managers can only update status for tasks belonging to their own team.");
        }

        if (task.AssignedToId == currentUserId)
        {
            return;
        }

        throw new ForbiddenException("Users can only update status for tasks assigned to themselves.");
    }

    private async Task ValidateTeamAndAssigneeAsync(Guid teamId, Guid assignedToId, CancellationToken cancellationToken)
    {
        if (!await _taskRepository.TeamExistsAsync(teamId, cancellationToken).ConfigureAwait(false))
        {
            throw new NotFoundException($"Team with ID '{teamId}' was not found.");
        }

        await ValidateAssigneeMembershipAsync(teamId, assignedToId, cancellationToken).ConfigureAwait(false);
    }

    private async Task ValidateAssigneeMembershipAsync(Guid teamId, Guid assignedToId, CancellationToken cancellationToken)
    {
        if (!await _taskRepository.UserExistsAsync(assignedToId, cancellationToken).ConfigureAwait(false))
        {
            throw new NotFoundException($"Assigned user with ID '{assignedToId}' was not found.");
        }

        var userTeamId = await _taskRepository.GetUserTeamIdAsync(assignedToId, cancellationToken).ConfigureAwait(false);
        if (userTeamId != teamId)
        {
            throw new ValidationException("The assigned user does not belong to the selected team.");
        }
    }
}

