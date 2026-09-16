using TaskManagement.Application.DTOs.Comments;

namespace TaskManagement.Application.Interfaces;

public interface ICommentService
{
    Task<CommentResponse> CreateCommentAsync(
        Guid taskId,
        CreateCommentRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommentResponse>> GetCommentsAsync(
        Guid taskId,
        CancellationToken cancellationToken = default);

    Task DeleteCommentAsync(
        Guid taskId,
        Guid commentId,
        CancellationToken cancellationToken = default);
}

