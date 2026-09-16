using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs.Notifications;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using Xunit;

namespace TaskManagement.UnitTests;

public sealed class NotificationServiceTests
{
    private readonly TestNotificationRepository _repository = new();
    private readonly TestCurrentUserService _currentUserService = new();
    private readonly NotificationService _notificationService;

    public NotificationServiceTests()
    {
        _notificationService = new NotificationService(_repository, _currentUserService);
    }

    [Fact]
    public async Task GetNotificationsAsync_WhenUnauthenticated_ThrowsAuthException()
    {
        _currentUserService.ClearUser();

        await Assert.ThrowsAsync<AuthException>(() =>
            _notificationService.GetNotificationsAsync(new NotificationQueryParameters()));
    }

    [Fact]
    public async Task GetNotificationsAsync_WithInvalidPage_ThrowsValidationException()
    {
        _currentUserService.SetUser(Guid.NewGuid(), "user@test.com", "User");

        await Assert.ThrowsAsync<ValidationException>(() =>
            _notificationService.GetNotificationsAsync(new NotificationQueryParameters { Page = 0 }));
    }

    [Fact]
    public async Task GetNotificationsAsync_WithInvalidPageSize_ThrowsValidationException()
    {
        _currentUserService.SetUser(Guid.NewGuid(), "user@test.com", "User");

        await Assert.ThrowsAsync<ValidationException>(() =>
            _notificationService.GetNotificationsAsync(new NotificationQueryParameters { PageSize = 101 }));
    }

    [Fact]
    public async Task GetNotificationsAsync_WithInvalidSortDirection_ThrowsValidationException()
    {
        _currentUserService.SetUser(Guid.NewGuid(), "user@test.com", "User");

        await Assert.ThrowsAsync<ValidationException>(() =>
            _notificationService.GetNotificationsAsync(new NotificationQueryParameters { SortDirection = "invalid" }));
    }

    [Fact]
    public async Task GetNotificationsAsync_ValidUser_ReturnsPagedNotifications()
    {
        var userId = Guid.NewGuid();
        _currentUserService.SetUser(userId, "user@test.com", "User");

        _repository.PagedResultToReturn = new PagedResult<NotificationResponse>
        {
            Items = [new NotificationResponse { Id = Guid.NewGuid(), UserId = userId, Message = "Test Message" }],
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };

        var result = await _notificationService.GetNotificationsAsync(new NotificationQueryParameters());

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("Test Message", result.Items[0].Message);
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenUnauthenticated_ThrowsAuthException()
    {
        _currentUserService.ClearUser();

        await Assert.ThrowsAsync<AuthException>(() =>
            _notificationService.MarkAsReadAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task MarkAsReadAsync_NonexistentNotification_ThrowsNotFoundException()
    {
        _currentUserService.SetUser(Guid.NewGuid(), "user@test.com", "User");

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _notificationService.MarkAsReadAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task MarkAsReadAsync_OtherUserNotification_ThrowsForbiddenException()
    {
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var notifId = Guid.NewGuid();

        _currentUserService.SetUser(user1, "user1@test.com", "User");

        _repository.NotificationsMap[notifId] = new Notification
        {
            Id = notifId,
            UserId = user2,
            Message = "Secret",
            IsRead = false
        };

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _notificationService.MarkAsReadAsync(notifId));
    }

    [Fact]
    public async Task MarkAsReadAsync_OwnNotification_MarksReadAndReturns()
    {
        var userId = Guid.NewGuid();
        var notifId = Guid.NewGuid();

        _currentUserService.SetUser(userId, "user@test.com", "User");

        var notif = new Notification
        {
            Id = notifId,
            UserId = userId,
            Message = "Task Assigned",
            IsRead = false,
            Type = NotificationType.Assignment,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _repository.NotificationsMap[notifId] = notif;

        var result = await _notificationService.MarkAsReadAsync(notifId);

        Assert.NotNull(result);
        Assert.True(result.IsRead);
        Assert.True(notif.IsRead);
    }

    [Fact]
    public async Task MarkAsReadAsync_AlreadyRead_IsIdempotent()
    {
        var userId = Guid.NewGuid();
        var notifId = Guid.NewGuid();

        _currentUserService.SetUser(userId, "user@test.com", "User");

        var notif = new Notification
        {
            Id = notifId,
            UserId = userId,
            Message = "Already read",
            IsRead = true,
            Type = NotificationType.StatusUpdate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _repository.NotificationsMap[notifId] = notif;

        var result = await _notificationService.MarkAsReadAsync(notifId);

        Assert.NotNull(result);
        Assert.True(result.IsRead);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_WhenUnauthenticated_ThrowsAuthException()
    {
        _currentUserService.ClearUser();

        await Assert.ThrowsAsync<AuthException>(() =>
            _notificationService.MarkAllAsReadAsync());
    }

    [Fact]
    public async Task MarkAllAsReadAsync_CallsRepositoryWithCurrentUserId()
    {
        var userId = Guid.NewGuid();
        _currentUserService.SetUser(userId, "user@test.com", "User");

        await _notificationService.MarkAllAsReadAsync();

        Assert.Contains(userId, _repository.MarkAllReadUsers);
    }

    [Fact]
    public async Task CreateNotificationAsync_WithValidData_CreatesNotification()
    {
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        await _notificationService.CreateNotificationAsync(
            userId,
            taskId,
            NotificationType.Assignment,
            "You have a new task");

        Assert.Single(_repository.CreatedNotifications);
        var created = _repository.CreatedNotifications[0];
        Assert.Equal(userId, created.UserId);
        Assert.Equal(taskId, created.TaskId);
        Assert.Equal(NotificationType.Assignment, created.Type);
        Assert.Equal("You have a new task", created.Message);
        Assert.False(created.IsRead);
    }

    [Fact]
    public async Task CreateNotificationAsync_WithEmptyUserIdOrMessage_DoesNotCreate()
    {
        await _notificationService.CreateNotificationAsync(
            Guid.Empty,
            Guid.NewGuid(),
            NotificationType.Assignment,
            "Message");

        await _notificationService.CreateNotificationAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            NotificationType.Assignment,
            "   ");

        await _notificationService.CreateNotificationAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            NotificationType.None,
            "Message");

        Assert.Empty(_repository.CreatedNotifications);
    }

    private sealed class TestNotificationRepository : INotificationRepository
    {
        public Dictionary<Guid, Notification> NotificationsMap { get; } = new();
        public List<Notification> CreatedNotifications { get; } = [];
        public List<Guid> MarkAllReadUsers { get; } = [];
        public PagedResult<NotificationResponse>? PagedResultToReturn { get; set; }

        public Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            NotificationsMap.TryGetValue(id, out var notif);
            return Task.FromResult(notif);
        }

        public Task<PagedResult<NotificationResponse>> GetPagedByUserIdAsync(
            Guid userId,
            NotificationQueryParameters parameters,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PagedResultToReturn ?? new PagedResult<NotificationResponse>());
        }

        public Task<Notification> CreateAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(notification);
            NotificationsMap[notification.Id] = notification;
            CreatedNotifications.Add(notification);
            return Task.FromResult(notification);
        }

        public Task<bool> UpdateAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(notification);
            NotificationsMap[notification.Id] = notification;
            return Task.FromResult(true);
        }

        public Task MarkAllAsReadByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            MarkAllReadUsers.Add(userId);
            foreach (var n in NotificationsMap.Values.Where(n => n.UserId == userId))
            {
                n.IsRead = true;
            }
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

