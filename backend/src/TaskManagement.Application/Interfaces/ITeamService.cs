using TaskManagement.Application.DTOs.Teams;

namespace TaskManagement.Application.Interfaces;

public interface ITeamService
{
    Task<TeamResponse> CreateTeamAsync(CreateTeamRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TeamResponse>> GetTeamsAsync(CancellationToken cancellationToken = default);

    Task<TeamDetailsResponse> GetTeamByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TeamResponse> UpdateTeamAsync(Guid id, UpdateTeamRequest request, CancellationToken cancellationToken = default);

    Task<TeamDetailsResponse> AddMemberAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task<TeamDetailsResponse> RemoveMemberAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}

