using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs.Dashboard;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Enums;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Application.Services;

public class DashboardService : IDashboardService
{
    private static readonly HashSet<string> AllowedSortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "DEADLINE",
        "CREATEDAT",
        "PRIORITY",
        "TITLE",
        "STATUS"
    };

    private readonly IDashboardRepository _dashboardRepository;
    private readonly ICurrentUserService _currentUserService;

    public DashboardService(
        IDashboardRepository dashboardRepository,
        ICurrentUserService currentUserService)
    {
        ArgumentNullException.ThrowIfNull(dashboardRepository);
        ArgumentNullException.ThrowIfNull(currentUserService);

        _dashboardRepository = dashboardRepository;
        _currentUserService = currentUserService;
    }

    public async Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserIdOrThrow();
        var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);

        if (_currentUserService.IsInRole(UserRole.Admin.ToString()))
        {
            return await _dashboardRepository.GetSummaryAsync(null, null, todayUtc, cancellationToken).ConfigureAwait(false);
        }

        if (_currentUserService.IsInRole(UserRole.Manager.ToString()))
        {
            var managedTeamId = await _dashboardRepository.GetManagedTeamIdAsync(currentUserId, cancellationToken).ConfigureAwait(false);
            if (!managedTeamId.HasValue)
            {
                return new DashboardSummaryResponse();
            }

            return await _dashboardRepository.GetSummaryAsync(managedTeamId.Value, null, todayUtc, cancellationToken).ConfigureAwait(false);
        }

        // User: strictly own assigned tasks
        return await _dashboardRepository.GetSummaryAsync(null, currentUserId, todayUtc, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PagedResult<DashboardTaskResponse>> GetTasksAsync(
        DashboardTaskQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var currentUserId = GetCurrentUserIdOrThrow();
        ValidateQueryParameters(parameters);

        if (_currentUserService.IsInRole(UserRole.Admin.ToString()))
        {
            return await _dashboardRepository.GetTasksPagedAsync(
                parameters,
                parameters.TeamId,
                parameters.AssignedToId,
                cancellationToken).ConfigureAwait(false);
        }

        if (_currentUserService.IsInRole(UserRole.Manager.ToString()))
        {
            var managedTeamId = await _dashboardRepository.GetManagedTeamIdAsync(currentUserId, cancellationToken).ConfigureAwait(false);
            if (!managedTeamId.HasValue)
            {
                return new PagedResult<DashboardTaskResponse>
                {
                    Items = [],
                    Page = parameters.Page,
                    PageSize = parameters.PageSize,
                    TotalCount = 0
                };
            }

            if (parameters.TeamId.HasValue && parameters.TeamId.Value != managedTeamId.Value)
            {
                throw new ForbiddenException("Managers can only view tasks belonging to their own team.");
            }

            return await _dashboardRepository.GetTasksPagedAsync(
                parameters,
                managedTeamId.Value,
                parameters.AssignedToId,
                cancellationToken).ConfigureAwait(false);
        }

        // User: strictly own assigned tasks (ignores arbitrary parameters.AssignedToId)
        return await _dashboardRepository.GetTasksPagedAsync(
            parameters,
            parameters.TeamId,
            currentUserId,
            cancellationToken).ConfigureAwait(false);
    }

    private Guid GetCurrentUserIdOrThrow()
    {
        return _currentUserService.UserId ?? throw new AuthException("User is not authenticated.");
    }

    private static void ValidateQueryParameters(DashboardTaskQueryParameters parameters)
    {
        if (parameters.Page < 1)
        {
            throw new ValidationException("Page must be greater than or equal to 1.");
        }

        if (parameters.PageSize < 1 || parameters.PageSize > 100)
        {
            throw new ValidationException("PageSize must be between 1 and 100.");
        }

        if (parameters.DeadlineFrom.HasValue && parameters.DeadlineTo.HasValue &&
            parameters.DeadlineFrom.Value > parameters.DeadlineTo.Value)
        {
            throw new ValidationException("DeadlineFrom cannot be greater than DeadlineTo.");
        }

        if (!string.IsNullOrWhiteSpace(parameters.Status) &&
            !Enum.TryParse<TaskStatus>(parameters.Status, true, out _))
        {
            throw new ValidationException("Invalid status filter.");
        }

        if (!string.IsNullOrWhiteSpace(parameters.Priority) &&
            !Enum.TryParse<TaskPriority>(parameters.Priority, true, out _))
        {
            throw new ValidationException("Invalid priority filter.");
        }

        if (!string.IsNullOrWhiteSpace(parameters.SortDirection) &&
            !string.Equals(parameters.SortDirection, "asc", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(parameters.SortDirection, "desc", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("SortDirection must be 'asc' or 'desc'.");
        }

        if (!string.IsNullOrWhiteSpace(parameters.SortBy) &&
            !AllowedSortFields.Contains(parameters.SortBy.Trim()))
        {
            throw new ValidationException("Invalid SortBy field.");
        }
    }
}

