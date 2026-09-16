using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs.Notifications;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUserService;

    public NotificationService(
        INotificationRepository notificationRepository,
        ICurrentUserService currentUserService)
    {
        ArgumentNullException.ThrowIfNull(notificationRepository);
        ArgumentNullException.ThrowIfNull(currentUserService);

        _notificationRepository = notificationRepository;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<NotificationResponse>> GetNotificationsAsync(
        NotificationQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var currentUserId = GetCurrentUserIdOrThrow();
        ValidateQueryParameters(parameters);

        return await _notificationRepository.GetPagedByUserIdAsync(
            currentUserId,
            parameters,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<NotificationResponse> MarkAsReadAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserIdOrThrow();

        var notification = await _notificationRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (notification is null)
        {
            throw new NotFoundException($"Notification with ID '{id}' was not found.");
        }

        if (notification.UserId != currentUserId)
        {
            throw new ForbiddenException("You do not have permission to access this notification.");
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.UpdatedAt = DateTime.UtcNow;
            await _notificationRepository.UpdateAsync(notification, cancellationToken).ConfigureAwait(false);
        }

        return new NotificationResponse
        {
            Id = notification.Id,
            UserId = notification.UserId,
            TaskId = notification.TaskId,
            Type = notification.Type.ToString(),
            Message = notification.Message,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt
        };
    }

    public async Task MarkAllAsReadAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserIdOrThrow();

        await _notificationRepository.MarkAllAsReadByUserIdAsync(currentUserId, cancellationToken).ConfigureAwait(false);
    }

    public async Task CreateNotificationAsync(
        Guid userId,
        Guid? taskId,
        NotificationType type,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(message) || type == NotificationType.None)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TaskId = taskId,
            Type = type,
            Message = message.Trim(),
            IsRead = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _notificationRepository.CreateAsync(notification, cancellationToken).ConfigureAwait(false);
    }

    private Guid GetCurrentUserIdOrThrow()
    {
        return _currentUserService.UserId ?? throw new AuthException("User is not authenticated.");
    }

    private static void ValidateQueryParameters(NotificationQueryParameters parameters)
    {
        if (parameters.Page < 1)
        {
            throw new ValidationException("Page must be greater than or equal to 1.");
        }

        if (parameters.PageSize < 1 || parameters.PageSize > 100)
        {
            throw new ValidationException("PageSize must be between 1 and 100.");
        }

        if (!string.IsNullOrWhiteSpace(parameters.SortDirection) &&
            !string.Equals(parameters.SortDirection, "asc", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(parameters.SortDirection, "desc", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("SortDirection must be 'asc' or 'desc'.");
        }
    }
}

