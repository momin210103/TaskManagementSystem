using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs.Users;

namespace TaskManagement.Application.Interfaces;

public interface IUserService
{
    Task<UserResponse> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<UserResponse>> GetUsersAsync(UserQueryParameters parameters, CancellationToken cancellationToken = default);

    Task<UserResponse> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<UserResponse> UpdateUserRoleAsync(Guid id, ChangeUserRoleRequest request, CancellationToken cancellationToken = default);

    Task<UserResponse> UpdateUserTeamAsync(Guid id, AssignUserTeamRequest request, CancellationToken cancellationToken = default);
}

