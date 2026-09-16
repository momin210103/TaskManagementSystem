using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs.Notifications;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Interfaces;

public interface INotificationService
{
    Task<PagedResult<NotificationResponse>> GetNotificationsAsync(
        NotificationQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<NotificationResponse> MarkAsReadAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(CancellationToken cancellationToken = default);

    Task CreateNotificationAsync(
        Guid userId,
        Guid? taskId,
        NotificationType type,
        string message,
        CancellationToken cancellationToken = default);
}

