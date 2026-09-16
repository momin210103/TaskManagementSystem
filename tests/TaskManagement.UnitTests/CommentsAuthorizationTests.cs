using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Application.DTOs.Comments;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Identity;
using TaskManagement.Infrastructure.Persistence;
using TaskManagement.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TaskManagement.UnitTests;

public sealed class CommentsAuthorizationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly CommentRepository _commentRepository;
    private readonly TestCurrentUserService _currentUserService;
    private readonly CommentService _commentService;

    public CommentsAuthorizationTests()
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

        _commentRepository = new CommentRepository(_dbContext);
        _currentUserService = new TestCurrentUserService();
        _commentService = new CommentService(_commentRepository, _currentUserService);

        DatabaseSeeder.SeedAsync(_serviceProvider).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task UnauthenticatedUser_CreateComment_ThrowsAuthException()
    {
        _currentUserService.ClearUser();

        await Assert.ThrowsAsync<AuthException>(() =>
            _commentService.CreateCommentAsync(Guid.NewGuid(), new CreateCommentRequest { Content = "Hello" }));
    }

    [Fact]
    public async Task Admin_CreateComment_OnAnyTask_Succeeds()
    {
        var admin = await _userManager.FindByEmailAsync("admin@taskmanagement.com");
        var task = await _dbContext.Tasks.FirstOrDefaultAsync();
        Assert.NotNull(admin);
        Assert.NotNull(task);

        _currentUserService.SetUser(admin.Id, admin.Email!, UserRole.Admin.ToString());

        var result = await _commentService.CreateCommentAsync(task.Id, new CreateCommentRequest
        {
            Content = "Admin system comment"
        });

        Assert.NotNull(result);
        Assert.Equal("Admin system comment", result.Content);
        Assert.Equal(admin.Id, result.UserId);
        Assert.Equal(admin.Name, result.UserName);
    }

    [Fact]
    public async Task Manager_CreateComment_OnOwnTeamTask_Succeeds()
    {
        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        var engTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "Engineering Team");
        var engTask = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.TeamId == engTeam!.Id);
        Assert.NotNull(manager1);
        Assert.NotNull(engTask);

        _currentUserService.SetUser(manager1.Id, manager1.Email!, UserRole.Manager.ToString());

        var result = await _commentService.CreateCommentAsync(engTask.Id, new CreateCommentRequest
        {
            Content = "Manager review comment"
        });

        Assert.NotNull(result);
        Assert.Equal(manager1.Id, result.UserId);
    }

    [Fact]
    public async Task Manager_CreateComment_OnOtherTeamTask_ThrowsForbidden_IDOR()
    {
        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        var qaTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "QA & Testing Team");
        var qaTask = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.TeamId == qaTeam!.Id);
        Assert.NotNull(manager1);
        Assert.NotNull(qaTask);

        _currentUserService.SetUser(manager1.Id, manager1.Email!, UserRole.Manager.ToString());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _commentService.CreateCommentAsync(qaTask.Id, new CreateCommentRequest { Content = "Unauthorized" }));
    }

    [Fact]
    public async Task User_CreateComment_OnAssignedTask_Succeeds()
    {
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        var dev1Task = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.AssignedToId == dev1!.Id);
        Assert.NotNull(dev1);
        Assert.NotNull(dev1Task);

        _currentUserService.SetUser(dev1.Id, dev1.Email!, UserRole.User.ToString());

        var result = await _commentService.CreateCommentAsync(dev1Task.Id, new CreateCommentRequest
        {
            Content = "Progress update"
        });

        Assert.NotNull(result);
        Assert.Equal(dev1.Id, result.UserId);
    }

    [Fact]
    public async Task User_CreateComment_OnOtherUserTask_ThrowsForbidden_IDOR()
    {
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        var dev2 = await _userManager.FindByEmailAsync("dev2@taskmanagement.com");
        var dev2Task = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.AssignedToId == dev2!.Id);
        Assert.NotNull(dev1);
        Assert.NotNull(dev2Task);

        _currentUserService.SetUser(dev1.Id, dev1.Email!, UserRole.User.ToString());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _commentService.CreateCommentAsync(dev2Task.Id, new CreateCommentRequest { Content = "Unauthorized" }));
    }

    [Fact]
    public async Task CreateComment_OnNonexistentTask_ThrowsNotFound()
    {
        var admin = await _userManager.FindByEmailAsync("admin@taskmanagement.com");
        Assert.NotNull(admin);

        _currentUserService.SetUser(admin.Id, admin.Email!, UserRole.Admin.ToString());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _commentService.CreateCommentAsync(Guid.NewGuid(), new CreateCommentRequest { Content = "Hello" }));
    }

    [Fact]
    public async Task Admin_GetComments_ReturnsAllCommentsInDeterministicOrder()
    {
        var admin = await _userManager.FindByEmailAsync("admin@taskmanagement.com");
        var team = await _dbContext.Teams.FirstOrDefaultAsync();
        Assert.NotNull(admin);
        Assert.NotNull(team);

        var dedicatedTask = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Ordering Test Task",
            Description = "Task for testing comment ordering",
            Status = TaskManagement.Domain.Enums.TaskStatus.ToDo,
            Priority = TaskPriority.Medium,
            Deadline = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            TeamId = team.Id,
            AssignedToId = admin.Id,
            AssignedById = admin.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Tasks.Add(dedicatedTask);
        await _dbContext.SaveChangesAsync();

        // Add two comments
        var c1 = new Comment { Id = Guid.NewGuid(), TaskId = dedicatedTask.Id, UserId = admin.Id, Content = "First", CreatedAt = DateTime.UtcNow.AddMinutes(-5), UpdatedAt = DateTime.UtcNow.AddMinutes(-5) };
        var c2 = new Comment { Id = Guid.NewGuid(), TaskId = dedicatedTask.Id, UserId = admin.Id, Content = "Second", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _dbContext.Comments.AddRange(c1, c2);
        await _dbContext.SaveChangesAsync();

        _currentUserService.SetUser(admin.Id, admin.Email!, UserRole.Admin.ToString());

        var comments = await _commentService.GetCommentsAsync(dedicatedTask.Id);

        Assert.NotNull(comments);
        Assert.Equal(2, comments.Count);
        Assert.Equal("First", comments[0].Content);
        Assert.Equal("Second", comments[1].Content);
    }

    [Fact]
    public async Task User_GetComments_ForOtherUserTask_ThrowsForbidden_IDOR()
    {
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        var dev2 = await _userManager.FindByEmailAsync("dev2@taskmanagement.com");
        var dev2Task = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.AssignedToId == dev2!.Id);
        Assert.NotNull(dev1);
        Assert.NotNull(dev2Task);

        _currentUserService.SetUser(dev1.Id, dev1.Email!, UserRole.User.ToString());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _commentService.GetCommentsAsync(dev2Task.Id));
    }

    [Fact]
    public async Task Admin_DeleteComment_Succeeds()
    {
        var admin = await _userManager.FindByEmailAsync("admin@taskmanagement.com");
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        var task = await _dbContext.Tasks.FirstOrDefaultAsync();
        Assert.NotNull(admin);
        Assert.NotNull(task);

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            TaskId = task.Id,
            UserId = dev1!.Id,
            Content = "To be deleted by Admin",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Comments.Add(comment);
        await _dbContext.SaveChangesAsync();

        _currentUserService.SetUser(admin.Id, admin.Email!, UserRole.Admin.ToString());

        await _commentService.DeleteCommentAsync(task.Id, comment.Id);

        var deleted = await _dbContext.Comments.FindAsync(comment.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task Manager_DeleteComment_OnOwnTeamTask_Succeeds()
    {
        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        var engTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "Engineering Team");
        var engTask = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.TeamId == engTeam!.Id);
        Assert.NotNull(manager1);
        Assert.NotNull(engTask);

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            TaskId = engTask.Id,
            UserId = dev1!.Id,
            Content = "Dev comment to delete",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Comments.Add(comment);
        await _dbContext.SaveChangesAsync();

        _currentUserService.SetUser(manager1.Id, manager1.Email!, UserRole.Manager.ToString());

        await _commentService.DeleteCommentAsync(engTask.Id, comment.Id);

        var deleted = await _dbContext.Comments.FindAsync(comment.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task User_DeleteComment_ForOwnComment_Succeeds()
    {
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        var dev1Task = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.AssignedToId == dev1!.Id);
        Assert.NotNull(dev1);
        Assert.NotNull(dev1Task);

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            TaskId = dev1Task.Id,
            UserId = dev1.Id,
            Content = "My comment",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Comments.Add(comment);
        await _dbContext.SaveChangesAsync();

        _currentUserService.SetUser(dev1.Id, dev1.Email!, UserRole.User.ToString());

        await _commentService.DeleteCommentAsync(dev1Task.Id, comment.Id);

        var deleted = await _dbContext.Comments.FindAsync(comment.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task User_DeleteComment_ForOtherUserComment_ThrowsForbidden_IDOR()
    {
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        var dev2 = await _userManager.FindByEmailAsync("dev2@taskmanagement.com");
        var dev1Task = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.AssignedToId == dev1!.Id);
        Assert.NotNull(dev1);
        Assert.NotNull(dev2);
        Assert.NotNull(dev1Task);

        var otherComment = new Comment
        {
            Id = Guid.NewGuid(),
            TaskId = dev1Task.Id,
            UserId = dev2.Id,
            Content = "Other user comment",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Comments.Add(otherComment);
        await _dbContext.SaveChangesAsync();

        _currentUserService.SetUser(dev1.Id, dev1.Email!, UserRole.User.ToString());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _commentService.DeleteCommentAsync(dev1Task.Id, otherComment.Id));
    }

    [Fact]
    public async Task DeleteComment_FromDifferentTask_ThrowsNotFound()
    {
        var admin = await _userManager.FindByEmailAsync("admin@taskmanagement.com");
        var tasks = await _dbContext.Tasks.Take(2).ToListAsync();
        Assert.NotNull(admin);
        Assert.True(tasks.Count >= 2);

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            TaskId = tasks[1].Id,
            UserId = admin.Id,
            Content = "Task 2 comment",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Comments.Add(comment);
        await _dbContext.SaveChangesAsync();

        _currentUserService.SetUser(admin.Id, admin.Email!, UserRole.Admin.ToString());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _commentService.DeleteCommentAsync(tasks[0].Id, comment.Id));
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
