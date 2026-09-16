using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs.Notifications;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly ApplicationDbContext _dbContext;

    public NotificationRepository(ApplicationDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<PagedResult<NotificationResponse>> GetPagedByUserIdAsync(
        Guid userId,
        NotificationQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var query = _dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        if (parameters.IsRead.HasValue)
        {
            query = query.Where(n => n.IsRead == parameters.IsRead.Value);
        }

        var isAsc = string.Equals(parameters.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        query = isAsc
            ? query.OrderBy(n => n.CreatedAt).ThenBy(n => n.Id)
            : query.OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await query
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(n => new NotificationResponse
            {
                Id = n.Id,
                UserId = n.UserId,
                TaskId = n.TaskId,
                Type = n.Type.ToString(),
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<NotificationResponse>
        {
            Items = items,
            Page = parameters.Page,
            PageSize = parameters.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<Notification> CreateAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        await _dbContext.Notifications.AddAsync(notification, cancellationToken).ConfigureAwait(false);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return notification;
    }

    public async Task<bool> UpdateAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        _dbContext.Notifications.Update(notification);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return true;
    }

    public async Task MarkAllAsReadByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var unreadNotifications = await _dbContext.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (unreadNotifications.Count > 0)
        {
            var now = DateTime.UtcNow;
            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.UpdatedAt = now;
            }

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}

