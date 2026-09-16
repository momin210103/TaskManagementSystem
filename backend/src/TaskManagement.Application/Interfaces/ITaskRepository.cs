using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Interfaces;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TaskResponse?> GetResponseByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<TaskResponse>> GetPagedAsync(
        TaskQueryParameters parameters,
        Guid? enforcedTeamId = null,
        Guid? enforcedAssignedToId = null,
        CancellationToken cancellationToken = default);

    Task<TaskItem> CreateAsync(TaskItem task, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(TaskItem task, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> TeamExistsAsync(Guid teamId, CancellationToken cancellationToken = default);

    Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Guid?> GetUserTeamIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Guid?> GetTeamManagerIdAsync(Guid teamId, CancellationToken cancellationToken = default);

    Task<Guid?> GetManagedTeamIdAsync(Guid managerId, CancellationToken cancellationToken = default);
}

