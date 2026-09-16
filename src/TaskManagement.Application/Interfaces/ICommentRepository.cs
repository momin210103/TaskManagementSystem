using TaskManagement.Application.DTOs.Comments;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Interfaces;

public interface ICommentRepository
{
    Task<Comment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CommentResponse?> GetResponseByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommentResponse>> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default);

    Task<Comment> CreateAsync(Comment comment, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TaskItem?> GetTaskByIdAsync(Guid taskId, CancellationToken cancellationToken = default);

    Task<Guid?> GetTeamManagerIdAsync(Guid teamId, CancellationToken cancellationToken = default);
}

