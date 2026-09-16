using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.DTOs.Comments;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Repositories;

public class CommentRepository : ICommentRepository
{
    private readonly ApplicationDbContext _context;

    public CommentRepository(ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task<Comment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Comments
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<CommentResponse?> GetResponseByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var comment = await _context.Comments
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (comment is null)
        {
            return null;
        }

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == comment.UserId, cancellationToken)
            .ConfigureAwait(false);

        return new CommentResponse
        {
            Id = comment.Id,
            TaskId = comment.TaskId,
            UserId = comment.UserId,
            UserName = user?.Name ?? string.Empty,
            UserEmail = user?.Email ?? string.Empty,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt
        };
    }

    public async Task<IReadOnlyList<CommentResponse>> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var comments = await _context.Comments
            .AsNoTracking()
            .Where(c => c.TaskId == taskId)
            .OrderBy(c => c.CreatedAt)
            .ThenBy(c => c.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (comments.Count == 0)
        {
            return [];
        }

        var userIds = comments.Select(c => c.UserId).Distinct().ToList();
        var users = await _context.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken)
            .ConfigureAwait(false);

        return comments.Select(c =>
        {
            users.TryGetValue(c.UserId, out var user);
            return new CommentResponse
            {
                Id = c.Id,
                TaskId = c.TaskId,
                UserId = c.UserId,
                UserName = user?.Name ?? string.Empty,
                UserEmail = user?.Email ?? string.Empty,
                Content = c.Content,
                CreatedAt = c.CreatedAt
            };
        }).ToList();
    }

    public async Task<Comment> CreateAsync(Comment comment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comment);

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return comment;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var comment = await _context.Comments
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (comment is null)
        {
            return false;
        }

        _context.Comments.Remove(comment);
        var affected = await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return affected > 0;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Comments
            .AnyAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TaskItem?> GetTaskByIdAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Guid?> GetTeamManagerIdAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        return await _context.Teams
            .Where(t => t.Id == teamId)
            .Select(t => (Guid?)t.ManagerId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

