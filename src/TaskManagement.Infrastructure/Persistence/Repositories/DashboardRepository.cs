using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs.Dashboard;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Infrastructure.Persistence.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly ApplicationDbContext _context;

    public DashboardRepository(ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task<DashboardSummaryResponse> GetSummaryAsync(
        Guid? enforcedTeamId,
        Guid? enforcedAssignedToId,
        DateOnly todayUtc,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Tasks.AsNoTracking().AsQueryable();

        if (enforcedTeamId.HasValue)
        {
            query = query.Where(t => t.TeamId == enforcedTeamId.Value);
        }

        if (enforcedAssignedToId.HasValue)
        {
            query = query.Where(t => t.AssignedToId == enforcedAssignedToId.Value);
        }

        var summary = await query
            .GroupBy(_ => 1)
            .Select(g => new DashboardSummaryResponse
            {
                TotalTasks = g.Count(),
                ToDoCount = g.Count(t => t.Status == TaskStatus.ToDo),
                InProgressCount = g.Count(t => t.Status == TaskStatus.InProgress),
                DoneCount = g.Count(t => t.Status == TaskStatus.Done),
                HighPriorityCount = g.Count(t => t.Priority == TaskPriority.High),
                OverdueCount = g.Count(t => t.Deadline < todayUtc && t.Status != TaskStatus.Done),
                DueTodayCount = g.Count(t => t.Deadline == todayUtc && t.Status != TaskStatus.Done),
                UpcomingCount = g.Count(t => t.Deadline > todayUtc && t.Status != TaskStatus.Done)
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return summary ?? new DashboardSummaryResponse();
    }

    public async Task<PagedResult<DashboardTaskResponse>> GetTasksPagedAsync(
        DashboardTaskQueryParameters parameters,
        Guid? enforcedTeamId,
        Guid? enforcedAssignedToId,
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

        var items = await MapToDashboardTaskResponsesAsync(tasks, cancellationToken).ConfigureAwait(false);

        return new PagedResult<DashboardTaskResponse>
        {
            Items = items,
            Page = effectivePage,
            PageSize = effectivePageSize,
            TotalCount = totalCount
        };
    }

    public async Task<Guid?> GetManagedTeamIdAsync(Guid managerId, CancellationToken cancellationToken = default)
    {
        return await _context.Teams
            .AsNoTracking()
            .Where(t => t.ManagerId == managerId)
            .Select(t => (Guid?)t.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static IQueryable<TaskItem> ApplyTeamAndAssigneeFilters(
        IQueryable<TaskItem> query,
        DashboardTaskQueryParameters parameters,
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
        DashboardTaskQueryParameters parameters)
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
            "TITLE" => isDescending ? query.OrderByDescending(t => t.Title).ThenByDescending(t => t.Id) : query.OrderBy(t => t.Title).ThenBy(t => t.Id),
            "STATUS" => isDescending ? query.OrderByDescending(t => t.Status).ThenByDescending(t => t.Id) : query.OrderBy(t => t.Status).ThenBy(t => t.Id),
            "PRIORITY" => isDescending ? query.OrderByDescending(t => t.Priority).ThenByDescending(t => t.Id) : query.OrderBy(t => t.Priority).ThenBy(t => t.Id),
            "DEADLINE" => isDescending ? query.OrderByDescending(t => t.Deadline).ThenByDescending(t => t.Id) : query.OrderBy(t => t.Deadline).ThenBy(t => t.Id),
            "CREATEDAT" => isDescending ? query.OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id) : query.OrderBy(t => t.CreatedAt).ThenBy(t => t.Id),
            _ => isDescending ? query.OrderByDescending(t => t.Deadline).ThenByDescending(t => t.Id) : query.OrderBy(t => t.Deadline).ThenBy(t => t.Id)
        };
    }

    private async Task<IReadOnlyList<DashboardTaskResponse>> MapToDashboardTaskResponsesAsync(
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

        return tasks.Select(t => new DashboardTaskResponse
        {
            Id = t.Id,
            Title = t.Title,
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

