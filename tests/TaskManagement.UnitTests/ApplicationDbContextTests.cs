using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using TaskManagement.Domain.Entities;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.UnitTests;

public class ApplicationDbContextTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=TestDb;Username=postgres;Password=postgres")
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public void Model_ShouldContainAllDomainEntities()
    {
        using var context = CreateContext();
        var model = context.Model;

        Assert.NotNull(model.FindEntityType(typeof(Team)));
        Assert.NotNull(model.FindEntityType(typeof(TaskItem)));
        Assert.NotNull(model.FindEntityType(typeof(Comment)));
        Assert.NotNull(model.FindEntityType(typeof(Notification)));
        Assert.NotNull(model.FindEntityType(typeof(RefreshToken)));
    }

    [Fact]
    public void Model_TableNames_ShouldMatchDesignSpecification()
    {
        using var context = CreateContext();
        var model = context.Model;

        Assert.Equal("Teams", model.FindEntityType(typeof(Team))?.GetTableName());
        Assert.Equal("Tasks", model.FindEntityType(typeof(TaskItem))?.GetTableName());
        Assert.Equal("Comments", model.FindEntityType(typeof(Comment))?.GetTableName());
        Assert.Equal("Notifications", model.FindEntityType(typeof(Notification))?.GetTableName());
        Assert.Equal("RefreshTokens", model.FindEntityType(typeof(RefreshToken))?.GetTableName());
    }

    [Fact]
    public void TaskItem_RelationshipsAndConstraints_ShouldBeConfiguredCorrectly()
    {
        using var context = CreateContext();
        var taskEntityType = context.Model.FindEntityType(typeof(TaskItem));

        Assert.NotNull(taskEntityType);

        var titleProp = taskEntityType.FindProperty(nameof(TaskItem.Title));
        Assert.NotNull(titleProp);
        Assert.False(titleProp.IsNullable);
        Assert.Equal(150, titleProp.GetMaxLength());

        var deadlineProp = taskEntityType.FindProperty(nameof(TaskItem.Deadline));
        Assert.NotNull(deadlineProp);
        Assert.Equal("date", deadlineProp.GetColumnType());

        var teamFk = taskEntityType.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Team));
        Assert.NotNull(teamFk);
        Assert.Equal(DeleteBehavior.Restrict, teamFk.DeleteBehavior);
    }

    [Fact]
    public void Comment_RelationshipsAndConstraints_ShouldBeConfiguredCorrectly()
    {
        using var context = CreateContext();
        var commentEntityType = context.Model.FindEntityType(typeof(Comment));

        Assert.NotNull(commentEntityType);

        var contentProp = commentEntityType.FindProperty(nameof(Comment.Content));
        Assert.NotNull(contentProp);
        Assert.False(contentProp.IsNullable);
        Assert.Equal("text", contentProp.GetColumnType());

        var taskFk = commentEntityType.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(TaskItem));
        Assert.NotNull(taskFk);
        Assert.Equal(DeleteBehavior.Cascade, taskFk.DeleteBehavior);
    }

    [Fact]
    public void Notification_RelationshipsAndConstraints_ShouldBeConfiguredCorrectly()
    {
        using var context = CreateContext();
        var notificationEntityType = context.Model.FindEntityType(typeof(Notification));

        Assert.NotNull(notificationEntityType);

        var messageProp = notificationEntityType.FindProperty(nameof(Notification.Message));
        Assert.NotNull(messageProp);
        Assert.Equal(255, messageProp.GetMaxLength());

        var isReadProp = notificationEntityType.FindProperty(nameof(Notification.IsRead));
        Assert.NotNull(isReadProp);
        Assert.Equal((object)false, isReadProp.GetDefaultValue());

        var taskFk = notificationEntityType.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(TaskItem));
        Assert.NotNull(taskFk);
        Assert.Equal(DeleteBehavior.SetNull, taskFk.DeleteBehavior);
    }

    [Fact]
    public void RefreshToken_Constraints_ShouldBeConfiguredCorrectly()
    {
        using var context = CreateContext();
        var refreshTokenEntityType = context.Model.FindEntityType(typeof(RefreshToken));

        Assert.NotNull(refreshTokenEntityType);

        var tokenHashProp = refreshTokenEntityType.FindProperty(nameof(RefreshToken.TokenHash));
        Assert.NotNull(tokenHashProp);
        Assert.Equal(500, tokenHashProp.GetMaxLength());

        Assert.Null(refreshTokenEntityType.FindProperty(nameof(RefreshToken.IsRevoked)));
        Assert.Null(refreshTokenEntityType.FindProperty(nameof(RefreshToken.IsExpired)));
        Assert.Null(refreshTokenEntityType.FindProperty(nameof(RefreshToken.IsActive)));
    }
}
