using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs.Dashboard;

namespace TaskManagement.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<DashboardTaskResponse>> GetTasksAsync(
        DashboardTaskQueryParameters parameters,
        CancellationToken cancellationToken = default);
}

