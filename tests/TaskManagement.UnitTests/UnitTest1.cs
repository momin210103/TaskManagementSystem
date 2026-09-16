using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.UnitTests;

public class DomainTests
{
    [Fact]
    public void TaskItem_DefaultValues_ShouldMatchDesignSpecification()
    {
        var task = new TaskItem();

        Assert.Equal(TaskStatus.ToDo, task.Status);
        Assert.Equal(TaskPriority.Medium, task.Priority);
        Assert.NotNull(task.Comments);
        Assert.Empty(task.Comments);
    }

    [Fact]
    public void Notification_DefaultValues_ShouldMatchDesignSpecification()
    {
        var notification = new Notification();

        Assert.Equal(NotificationType.Assignment, notification.Type);
        Assert.False(notification.IsRead);
    }

    [Theory]
    [InlineData(NotificationType.Assignment, 1)]
    [InlineData(NotificationType.StatusUpdate, 2)]
    public void NotificationType_EnumValues_ShouldMatchDesign(NotificationType type, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)type);
    }

    [Theory]
    [InlineData(TaskStatus.ToDo, 1)]
    [InlineData(TaskStatus.InProgress, 2)]
    [InlineData(TaskStatus.Done, 3)]
    public void TaskStatus_EnumValues_ShouldMatchDesign(TaskStatus status, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)status);
    }

    [Theory]
    [InlineData(TaskPriority.Low, 1)]
    [InlineData(TaskPriority.Medium, 2)]
    [InlineData(TaskPriority.High, 3)]
    public void TaskPriority_EnumValues_ShouldMatchDesign(TaskPriority priority, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)priority);
    }

    [Theory]
    [InlineData(UserRole.Admin, 1)]
    [InlineData(UserRole.Manager, 2)]
    [InlineData(UserRole.User, 3)]
    public void UserRole_EnumValues_ShouldMatchDesign(UserRole role, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)role);
    }

    [Fact]
    public void RefreshToken_IsActive_ShouldBeTrue_WhenNotRevokedAndNotExpired()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            RevokedAt = null
        };

        Assert.True(token.IsActive);
        Assert.False(token.IsRevoked);
        Assert.False(token.IsExpired);
    }

    [Fact]
    public void RefreshToken_IsActive_ShouldBeFalse_WhenRevoked()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            RevokedAt = DateTime.UtcNow
        };

        Assert.False(token.IsActive);
        Assert.True(token.IsRevoked);
    }

    [Fact]
    public void RefreshToken_IsActive_ShouldBeFalse_WhenExpired()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            RevokedAt = null
        };

        Assert.False(token.IsActive);
        Assert.True(token.IsExpired);
    }
}
