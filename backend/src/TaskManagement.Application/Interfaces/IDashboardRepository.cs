using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs.Dashboard;

namespace TaskManagement.Application.Interfaces;

public interface IDashboardRepository
{
    Task<DashboardSummaryResponse> GetSummaryAsync(
        Guid? enforcedTeamId,
        Guid? enforcedAssignedToId,
        DateOnly todayUtc,
        CancellationToken cancellationToken = default);

    Task<PagedResult<DashboardTaskResponse>> GetTasksPagedAsync(
        DashboardTaskQueryParameters parameters,
        Guid? enforcedTeamId,
        Guid? enforcedAssignedToId,
        CancellationToken cancellationToken = default);

    Task<Guid?> GetManagedTeamIdAsync(Guid managerId, CancellationToken cancellationToken = default);
}

