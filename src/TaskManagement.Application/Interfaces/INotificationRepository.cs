using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs.Notifications;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Interfaces;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<NotificationResponse>> GetPagedByUserIdAsync(
        Guid userId,
        NotificationQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<Notification> CreateAsync(Notification notification, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(Notification notification, CancellationToken cancellationToken = default);

    Task MarkAllAsReadByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}

