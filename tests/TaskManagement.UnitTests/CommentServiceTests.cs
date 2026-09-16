using TaskManagement.Application.DTOs.Comments;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using Xunit;

namespace TaskManagement.UnitTests;

public sealed class CommentServiceTests
{
    private readonly TestCommentRepository _commentRepository = new();
    private readonly TestCurrentUserService _currentUserService = new();
    private readonly CommentService _commentService;

    public CommentServiceTests()
    {
        _commentService = new CommentService(_commentRepository, _currentUserService);
    }

    [Fact]
    public async Task CreateCommentAsync_AsAdmin_OnAnyTask_Succeeds()
    {
        var adminId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        _currentUserService.SetUser(adminId, "admin@test.com", UserRole.Admin.ToString());
        _commentRepository.TasksMap[taskId] = new TaskItem
        {
            Id = taskId,
            Title = "Some Task",
            TeamId = Guid.NewGuid(),
            AssignedToId = Guid.NewGuid()
        };

        var request = new CreateCommentRequest { Content = "Great progress!" };

        var result = await _commentService.CreateCommentAsync(taskId, request);

        Assert.NotNull(result);
        Assert.Equal("Great progress!", result.Content);
        Assert.Equal(adminId, result.UserId);
        Assert.Equal(taskId, result.TaskId);
    }

    [Fact]
    public async Task CreateCommentAsync_AsManager_OnOwnTeamTask_Succeeds()
    {
        var managerId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        _currentUserService.SetUser(managerId, "mgr@test.com", UserRole.Manager.ToString());
        _commentRepository.TeamManagerMap[teamId] = managerId;
        _commentRepository.TasksMap[taskId] = new TaskItem
        {
            Id = taskId,
            Title = "Team Task",
            TeamId = teamId,
            AssignedToId = Guid.NewGuid()
        };

        var result = await _commentService.CreateCommentAsync(taskId, new CreateCommentRequest { Content = "Please review this." });

        Assert.NotNull(result);
        Assert.Equal(managerId, result.UserId);
    }

    [Fact]
    public async Task CreateCommentAsync_AsManager_OnOtherTeamTask_ThrowsForbidden_IDOR()
    {
        var managerId = Guid.NewGuid();
        var ownTeamId = Guid.NewGuid();
        var otherTeamId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        _currentUserService.SetUser(managerId, "mgr@test.com", UserRole.Manager.ToString());
        _commentRepository.TeamManagerMap[ownTeamId] = managerId;
        _commentRepository.TeamManagerMap[otherTeamId] = Guid.NewGuid();
        _commentRepository.TasksMap[taskId] = new TaskItem
        {
            Id = taskId,
            Title = "Other Team Task",
            TeamId = otherTeamId,
            AssignedToId = Guid.NewGuid()
        };

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _commentService.CreateCommentAsync(taskId, new CreateCommentRequest { Content = "Unauthorized comment" }));
    }

    [Fact]
    public async Task CreateCommentAsync_AsUser_OnAssignedTask_Succeeds()
    {
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        _currentUserService.SetUser(userId, "user@test.com", UserRole.User.ToString());
        _commentRepository.TasksMap[taskId] = new TaskItem
        {
            Id = taskId,
            Title = "My Task",
            TeamId = Guid.NewGuid(),
            AssignedToId = userId
        };

        var result = await _commentService.CreateCommentAsync(taskId, new CreateCommentRequest { Content = "Working on it." });

        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
    }

    [Fact]
    public async Task CreateCommentAsync_AsUser_OnOtherUserTask_ThrowsForbidden_IDOR()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        _currentUserService.SetUser(userId, "user@test.com", UserRole.User.ToString());
        _commentRepository.TasksMap[taskId] = new TaskItem
        {
            Id = taskId,
            Title = "Other Task",
            TeamId = Guid.NewGuid(),
            AssignedToId = otherUserId
        };

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _commentService.CreateCommentAsync(taskId, new CreateCommentRequest { Content = "Spying comment" }));
    }

    [Fact]
    public async Task CreateCommentAsync_WhenUnauthenticated_ThrowsAuthException()
    {
        _currentUserService.ClearUser();

        await Assert.ThrowsAsync<AuthException>(() =>
            _commentService.CreateCommentAsync(Guid.NewGuid(), new CreateCommentRequest { Content = "Anonymous comment" }));
    }

    [Fact]
    public async Task CreateCommentAsync_OnNonexistentTask_ThrowsNotFound()
    {
        var adminId = Guid.NewGuid();
        _currentUserService.SetUser(adminId, "admin@test.com", UserRole.Admin.ToString());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _commentService.CreateCommentAsync(Guid.NewGuid(), new CreateCommentRequest { Content = "Test" }));
    }

    [Fact]
    public async Task CreateCommentAsync_WithEmptyContent_ThrowsValidation()
    {
        var adminId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        _currentUserService.SetUser(adminId, "admin@test.com", UserRole.Admin.ToString());
        _commentRepository.TasksMap[taskId] = new TaskItem { Id = taskId, Title = "Task", TeamId = Guid.NewGuid() };

        await Assert.ThrowsAsync<ValidationException>(() =>
            _commentService.CreateCommentAsync(taskId, new CreateCommentRequest { Content = "   " }));
    }

    [Fact]
    public async Task CreateCommentAsync_WithOversizedContent_ThrowsValidation()
    {
        var adminId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        _currentUserService.SetUser(adminId, "admin@test.com", UserRole.Admin.ToString());
        _commentRepository.TasksMap[taskId] = new TaskItem { Id = taskId, Title = "Task", TeamId = Guid.NewGuid() };

        var oversizedContent = new string('A', 2001);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _commentService.CreateCommentAsync(taskId, new CreateCommentRequest { Content = oversizedContent }));
    }

    [Fact]
    public async Task GetCommentsAsync_AsAdmin_ReturnsComments()
    {
        var adminId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        _currentUserService.SetUser(adminId, "admin@test.com", UserRole.Admin.ToString());
        _commentRepository.TasksMap[taskId] = new TaskItem { Id = taskId, Title = "Task", TeamId = Guid.NewGuid() };
        _commentRepository.CommentsList.Add(new CommentResponse { Id = Guid.NewGuid(), TaskId = taskId, Content = "Comment 1" });

        var result = await _commentService.GetCommentsAsync(taskId);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetCommentsAsync_AsUser_ForOtherUserTask_ThrowsForbidden_IDOR()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        _currentUserService.SetUser(userId, "user@test.com", UserRole.User.ToString());
        _commentRepository.TasksMap[taskId] = new TaskItem { Id = taskId, Title = "Other Task", TeamId = Guid.NewGuid(), AssignedToId = otherUserId };

        await Assert.ThrowsAsync<ForbiddenException>(() => _commentService.GetCommentsAsync(taskId));
    }

    [Fact]
    public async Task DeleteCommentAsync_AsAdmin_Succeeds()
    {
        var adminId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var commentId = Guid.NewGuid();

        _currentUserService.SetUser(adminId, "admin@test.com", UserRole.Admin.ToString());
        _commentRepository.TasksMap[taskId] = new TaskItem { Id = taskId, Title = "Task", TeamId = Guid.NewGuid() };
        _commentRepository.CommentsMap[commentId] = new Comment { Id = commentId, TaskId = taskId, UserId = Guid.NewGuid() };

        await _commentService.DeleteCommentAsync(taskId, commentId);

        Assert.False(_commentRepository.CommentsMap.ContainsKey(commentId));
    }

    [Fact]
    public async Task DeleteCommentAsync_AsManager_OnOwnTeamTask_Succeeds()
    {
        var managerId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var commentId = Guid.NewGuid();

        _currentUserService.SetUser(managerId, "mgr@test.com", UserRole.Manager.ToString());
        _commentRepository.TeamManagerMap[teamId] = managerId;
        _commentRepository.TasksMap[taskId] = new TaskItem { Id = taskId, Title = "Task", TeamId = teamId };
        _commentRepository.CommentsMap[commentId] = new Comment { Id = commentId, TaskId = taskId, UserId = Guid.NewGuid() };

        await _commentService.DeleteCommentAsync(taskId, commentId);

        Assert.False(_commentRepository.CommentsMap.ContainsKey(commentId));
    }

    [Fact]
    public async Task DeleteCommentAsync_AsUser_ForOwnComment_Succeeds()
    {
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var commentId = Guid.NewGuid();

        _currentUserService.SetUser(userId, "user@test.com", UserRole.User.ToString());
        _commentRepository.TasksMap[taskId] = new TaskItem { Id = taskId, Title = "Task", TeamId = Guid.NewGuid(), AssignedToId = userId };
        _commentRepository.CommentsMap[commentId] = new Comment { Id = commentId, TaskId = taskId, UserId = userId };

        await _commentService.DeleteCommentAsync(taskId, commentId);

        Assert.False(_commentRepository.CommentsMap.ContainsKey(commentId));
    }

    [Fact]
    public async Task DeleteCommentAsync_AsUser_ForOtherUserComment_ThrowsForbidden_IDOR()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var commentId = Guid.NewGuid();

        _currentUserService.SetUser(userId, "user@test.com", UserRole.User.ToString());
        _commentRepository.TasksMap[taskId] = new TaskItem { Id = taskId, Title = "Task", TeamId = Guid.NewGuid(), AssignedToId = userId };
        _commentRepository.CommentsMap[commentId] = new Comment { Id = commentId, TaskId = taskId, UserId = otherUserId };

        await Assert.ThrowsAsync<ForbiddenException>(() => _commentService.DeleteCommentAsync(taskId, commentId));
    }

    [Fact]
    public async Task DeleteCommentAsync_WhenCommentBelongsToDifferentTask_ThrowsNotFound()
    {
        var adminId = Guid.NewGuid();
        var task1Id = Guid.NewGuid();
        var task2Id = Guid.NewGuid();
        var commentId = Guid.NewGuid();

        _currentUserService.SetUser(adminId, "admin@test.com", UserRole.Admin.ToString());
        _commentRepository.TasksMap[task1Id] = new TaskItem { Id = task1Id, Title = "Task 1", TeamId = Guid.NewGuid() };
        _commentRepository.TasksMap[task2Id] = new TaskItem { Id = task2Id, Title = "Task 2", TeamId = Guid.NewGuid() };
        _commentRepository.CommentsMap[commentId] = new Comment { Id = commentId, TaskId = task2Id, UserId = Guid.NewGuid() };

        await Assert.ThrowsAsync<NotFoundException>(() => _commentService.DeleteCommentAsync(task1Id, commentId));
    }

    private sealed class TestCommentRepository : ICommentRepository
    {
        public Dictionary<Guid, Comment> CommentsMap { get; } = new();
        public Dictionary<Guid, TaskItem> TasksMap { get; } = new();
        public Dictionary<Guid, Guid> TeamManagerMap { get; } = new();
        public List<CommentResponse> CommentsList { get; } = [];

        public Task<Comment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            CommentsMap.TryGetValue(id, out var comment);
            return Task.FromResult(comment);
        }

        public Task<CommentResponse?> GetResponseByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (CommentsMap.TryGetValue(id, out var comment))
            {
                return Task.FromResult<CommentResponse?>(new CommentResponse
                {
                    Id = comment.Id,
                    TaskId = comment.TaskId,
                    UserId = comment.UserId,
                    Content = comment.Content,
                    CreatedAt = comment.CreatedAt
                });
            }
            return Task.FromResult<CommentResponse?>(null);
        }

        public Task<IReadOnlyList<CommentResponse>> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<CommentResponse>>(CommentsList);
        }

        public Task<Comment> CreateAsync(Comment comment, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(comment);
            CommentsMap[comment.Id] = comment;
            return Task.FromResult(comment);
        }

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CommentsMap.Remove(id));
        }

        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CommentsMap.ContainsKey(id));
        }

        public Task<TaskItem?> GetTaskByIdAsync(Guid taskId, CancellationToken cancellationToken = default)
        {
            TasksMap.TryGetValue(taskId, out var task);
            return Task.FromResult(task);
        }

        public Task<Guid?> GetTeamManagerIdAsync(Guid teamId, CancellationToken cancellationToken = default)
        {
            if (TeamManagerMap.TryGetValue(teamId, out var managerId))
            {
                return Task.FromResult<Guid?>(managerId);
            }
            return Task.FromResult<Guid?>(null);
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

