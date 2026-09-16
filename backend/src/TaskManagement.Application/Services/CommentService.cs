using TaskManagement.Application.DTOs.Comments;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Services;

public class CommentService : ICommentService
{
    private readonly ICommentRepository _commentRepository;
    private readonly ICurrentUserService _currentUserService;

    public CommentService(ICommentRepository commentRepository, ICurrentUserService currentUserService)
    {
        ArgumentNullException.ThrowIfNull(commentRepository);
        ArgumentNullException.ThrowIfNull(currentUserService);

        _commentRepository = commentRepository;
        _currentUserService = currentUserService;
    }

    public async Task<CommentResponse> CreateCommentAsync(
        Guid taskId,
        CreateCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var currentUserId = GetCurrentUserIdOrThrow();

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new ValidationException("Comment content is required.");
        }

        if (request.Content.Trim().Length > 2000)
        {
            throw new ValidationException("Comment content cannot exceed 2000 characters.");
        }

        var task = await _commentRepository.GetTaskByIdAsync(taskId, cancellationToken).ConfigureAwait(false);
        if (task is null)
        {
            throw new NotFoundException($"Task with ID '{taskId}' was not found.");
        }

        await AuthorizeTaskAccessAsync(task, currentUserId, cancellationToken).ConfigureAwait(false);

        var now = DateTime.UtcNow;
        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            UserId = currentUserId,
            Content = request.Content.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        await _commentRepository.CreateAsync(comment, cancellationToken).ConfigureAwait(false);

        return await _commentRepository.GetResponseByIdAsync(comment.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Comment with ID '{comment.Id}' was not found.");
    }

    public async Task<IReadOnlyList<CommentResponse>> GetCommentsAsync(
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserIdOrThrow();

        var task = await _commentRepository.GetTaskByIdAsync(taskId, cancellationToken).ConfigureAwait(false);
        if (task is null)
        {
            throw new NotFoundException($"Task with ID '{taskId}' was not found.");
        }

        await AuthorizeTaskAccessAsync(task, currentUserId, cancellationToken).ConfigureAwait(false);

        return await _commentRepository.GetByTaskIdAsync(taskId, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteCommentAsync(
        Guid taskId,
        Guid commentId,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserIdOrThrow();

        var task = await _commentRepository.GetTaskByIdAsync(taskId, cancellationToken).ConfigureAwait(false);
        if (task is null)
        {
            throw new NotFoundException($"Task with ID '{taskId}' was not found.");
        }

        await AuthorizeTaskAccessAsync(task, currentUserId, cancellationToken).ConfigureAwait(false);

        var comment = await _commentRepository.GetByIdAsync(commentId, cancellationToken).ConfigureAwait(false);
        if (comment is null || comment.TaskId != taskId)
        {
            throw new NotFoundException($"Comment with ID '{commentId}' was not found for task '{taskId}'.");
        }

        AuthorizeCommentDeletion(comment, currentUserId);

        await _commentRepository.DeleteAsync(commentId, cancellationToken).ConfigureAwait(false);
    }

    private Guid GetCurrentUserIdOrThrow()
    {
        return _currentUserService.UserId ?? throw new AuthException("User is not authenticated.");
    }

    private async Task AuthorizeTaskAccessAsync(TaskItem task, Guid currentUserId, CancellationToken cancellationToken)
    {
        if (_currentUserService.IsInRole(UserRole.Admin.ToString()))
        {
            return;
        }

        if (_currentUserService.IsInRole(UserRole.Manager.ToString()))
        {
            var teamManagerId = await _commentRepository.GetTeamManagerIdAsync(task.TeamId, cancellationToken).ConfigureAwait(false);
            if (teamManagerId == currentUserId)
            {
                return;
            }

            throw new ForbiddenException("Managers can only access comments for tasks belonging to their own team.");
        }

        if (task.AssignedToId == currentUserId)
        {
            return;
        }

        throw new ForbiddenException("Users can only access comments for tasks assigned to themselves.");
    }

    private void AuthorizeCommentDeletion(Comment comment, Guid currentUserId)
    {
        if (_currentUserService.IsInRole(UserRole.Admin.ToString()))
        {
            return;
        }

        if (_currentUserService.IsInRole(UserRole.Manager.ToString()))
        {
            return;
        }

        if (comment.UserId != currentUserId)
        {
            throw new ForbiddenException("Users can only delete their own comments.");
        }
    }
}

