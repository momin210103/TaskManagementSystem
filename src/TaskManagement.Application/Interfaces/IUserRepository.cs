using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs.Users;

namespace TaskManagement.Application.Interfaces;

public interface IUserRepository
{
    Task<UserResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<UserResponse?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<PagedResult<UserResponse>> GetPagedAsync(
        int page,
        int pageSize,
        Guid? teamId = null,
        string? role = null,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> TeamExistsAsync(Guid teamId, CancellationToken cancellationToken = default);

    Task<Guid?> GetUserTeamIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> UpdateRoleAsync(Guid userId, string newRole, CancellationToken cancellationToken = default);

    Task<bool> UpdateTeamAsync(Guid userId, Guid? teamId, CancellationToken cancellationToken = default);
}

