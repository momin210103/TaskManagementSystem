using TaskManagement.Application.DTOs.Teams;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Interfaces;

public interface ITeamRepository
{
    Task<Team?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TeamResponse?> GetResponseByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TeamDetailsResponse?> GetDetailsByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TeamResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TeamResponse>> GetByManagerIdAsync(Guid managerId, CancellationToken cancellationToken = default);

    Task<TeamResponse?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(string name, Guid? excludeTeamId = null, CancellationToken cancellationToken = default);

    Task<Team> CreateAsync(Team team, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(Team team, CancellationToken cancellationToken = default);

    Task<bool> AddMemberAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default);

    Task<bool> RemoveMemberAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default);

    Task<bool> IsUserInRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default);

    Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Guid?> GetUserTeamIdAsync(Guid userId, CancellationToken cancellationToken = default);
}

