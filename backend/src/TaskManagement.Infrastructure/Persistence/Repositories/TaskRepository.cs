using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Infrastructure.Persistence.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly ApplicationDbContext _context;

    public TaskRepository(ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TaskResponse?> GetResponseByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _context.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (task is null)
        {
            return null;
        }

        var team = await _context.Teams
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == task.TeamId, cancellationToken)
            .ConfigureAwait(false);

        var assignee = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == task.AssignedToId, cancellationToken)
            .ConfigureAwait(false);

        var assigner = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == task.AssignedById, cancellationToken)
            .ConfigureAwait(false);

        return new TaskResponse
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status.ToString(),
            Priority = task.Priority.ToString(),
            Deadline = task.Deadline,
            TeamId = task.TeamId,
            TeamName = team?.Name ?? string.Empty,
            AssignedToId = task.AssignedToId,
            AssignedToName = assignee?.Name ?? string.Empty,
            AssignedById = task.AssignedById,
            AssignedByName = assigner?.Name ?? string.Empty,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };
    }

    public async Task<PagedResult<TaskResponse>> GetPagedAsync(
        TaskQueryParameters parameters,
        Guid? enforcedTeamId = null,
        Guid? enforcedAssignedToId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var query = _context.Tasks.AsNoTracking().AsQueryable();

        query = ApplyTeamAndAssigneeFilters(query, parameters, enforcedTeamId, enforcedAssignedToId);
        query = ApplyStatusPriorityAndDeadlineFilters(query, parameters);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var effectivePage = Math.Max(1, parameters.Page);
        var effectivePageSize = Math.Clamp(parameters.PageSize, 1, 100);

        query = ApplySorting(query, parameters.SortBy, parameters.SortDirection);

        var tasks = await query
            .Skip((effectivePage - 1) * effectivePageSize)
            .Take(effectivePageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = await MapToTaskResponsesAsync(tasks, cancellationToken).ConfigureAwait(false);

        return new PagedResult<TaskResponse>
        {
            Items = items,
            Page = effectivePage,
            PageSize = effectivePageSize,
            TotalCount = totalCount
        };
    }

    public async Task<TaskItem> CreateAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return task;
    }

    public async Task<bool> UpdateAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);

        _context.Tasks.Update(task);
        var affected = await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (task is null)
        {
            return false;
        }

        _context.Tasks.Remove(task);
        var affected = await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return affected > 0;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tasks
            .AnyAsync(t => t.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> TeamExistsAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        return await _context.Teams
            .AnyAsync(t => t.Id == teamId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AnyAsync(u => u.Id == userId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Guid?> GetUserTeamIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.TeamId)
            .FirstOrDefaultAsync(cancellationToken)
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

    public async Task<Guid?> GetManagedTeamIdAsync(Guid managerId, CancellationToken cancellationToken = default)
    {
        return await _context.Teams
            .Where(t => t.ManagerId == managerId)
            .Select(t => (Guid?)t.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static IQueryable<TaskItem> ApplyTeamAndAssigneeFilters(
        IQueryable<TaskItem> query,
        TaskQueryParameters parameters,
        Guid? enforcedTeamId,
        Guid? enforcedAssignedToId)
    {
        if (enforcedTeamId.HasValue)
        {
            query = query.Where(t => t.TeamId == enforcedTeamId.Value);
        }
        else if (parameters.TeamId.HasValue)
        {
            query = query.Where(t => t.TeamId == parameters.TeamId.Value);
        }

        if (enforcedAssignedToId.HasValue)
        {
            query = query.Where(t => t.AssignedToId == enforcedAssignedToId.Value);
        }
        else if (parameters.AssignedToId.HasValue)
        {
            query = query.Where(t => t.AssignedToId == parameters.AssignedToId.Value);
        }

        return query;
    }

    private static IQueryable<TaskItem> ApplyStatusPriorityAndDeadlineFilters(
        IQueryable<TaskItem> query,
        TaskQueryParameters parameters)
    {
        if (!string.IsNullOrWhiteSpace(parameters.Status) &&
            Enum.TryParse<TaskStatus>(parameters.Status, true, out var parsedStatus))
        {
            query = query.Where(t => t.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Priority) &&
            Enum.TryParse<TaskPriority>(parameters.Priority, true, out var parsedPriority))
        {
            query = query.Where(t => t.Priority == parsedPriority);
        }

        if (parameters.Deadline.HasValue)
        {
            query = query.Where(t => t.Deadline == parameters.Deadline.Value);
        }

        if (parameters.DeadlineFrom.HasValue)
        {
            query = query.Where(t => t.Deadline >= parameters.DeadlineFrom.Value);
        }

        if (parameters.DeadlineTo.HasValue)
        {
            query = query.Where(t => t.Deadline <= parameters.DeadlineTo.Value);
        }

        return query;
    }

    private static IQueryable<TaskItem> ApplySorting(
        IQueryable<TaskItem> query,
        string? sortBy,
        string? sortDirection)
    {
        var isDescending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return (sortBy?.ToUpperInvariant()) switch
        {
            "TITLE" => isDescending ? query.OrderByDescending(t => t.Title) : query.OrderBy(t => t.Title),
            "STATUS" => isDescending ? query.OrderByDescending(t => t.Status) : query.OrderBy(t => t.Status),
            "PRIORITY" => isDescending ? query.OrderByDescending(t => t.Priority) : query.OrderBy(t => t.Priority),
            "DEADLINE" => isDescending ? query.OrderByDescending(t => t.Deadline) : query.OrderBy(t => t.Deadline),
            "CREATEDAT" => isDescending ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt),
            "UPDATEDAT" => isDescending ? query.OrderByDescending(t => t.UpdatedAt) : query.OrderBy(t => t.UpdatedAt),
            _ => isDescending ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.Deadline).ThenByDescending(t => t.CreatedAt)
        };
    }

    private async Task<IReadOnlyList<TaskResponse>> MapToTaskResponsesAsync(
        List<TaskItem> tasks,
        CancellationToken cancellationToken)
    {
        if (tasks.Count == 0)
        {
            return [];
        }

        var teamIds = tasks.Select(t => t.TeamId).Distinct().ToList();
        var userIds = tasks.Select(t => t.AssignedToId).Concat(tasks.Select(t => t.AssignedById)).Distinct().ToList();

        var teams = await _context.Teams
            .AsNoTracking()
            .Where(t => teamIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken)
            .ConfigureAwait(false);

        var users = await _context.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken)
            .ConfigureAwait(false);

        return tasks.Select(t => new TaskResponse
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description,
            Status = t.Status.ToString(),
            Priority = t.Priority.ToString(),
            Deadline = t.Deadline,
            TeamId = t.TeamId,
            TeamName = teams.TryGetValue(t.TeamId, out var teamName) ? teamName : string.Empty,
            AssignedToId = t.AssignedToId,
            AssignedToName = users.TryGetValue(t.AssignedToId, out var assigneeName) ? assigneeName : string.Empty,
            AssignedById = t.AssignedById,
            AssignedByName = users.TryGetValue(t.AssignedById, out var assignerName) ? assignerName : string.Empty,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        }).ToList();
    }
}

