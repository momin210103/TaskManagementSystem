using TaskManagement.Application.DTOs.Teams;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Services;

public class TeamService : ITeamService
{
    private readonly ITeamRepository _teamRepository;
    private readonly ICurrentUserService _currentUserService;

    public TeamService(ITeamRepository teamRepository, ICurrentUserService currentUserService)
    {
        ArgumentNullException.ThrowIfNull(teamRepository);
        ArgumentNullException.ThrowIfNull(currentUserService);

        _teamRepository = teamRepository;
        _currentUserService = currentUserService;
    }

    public async Task<TeamResponse> CreateTeamAsync(CreateTeamRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_currentUserService.IsInRole(UserRole.Admin.ToString()))
        {
            throw new ForbiddenException("Only Admins can create teams.");
        }

        ValidateTeamName(request.Name);

        if (await _teamRepository.NameExistsAsync(request.Name, null, cancellationToken).ConfigureAwait(false))
        {
            throw new ConflictException($"A team with the name '{request.Name}' already exists.");
        }

        await ValidateManagerAsync(request.ManagerId, cancellationToken).ConfigureAwait(false);

        var now = DateTime.UtcNow;
        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            ManagerId = request.ManagerId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _teamRepository.CreateAsync(team, cancellationToken).ConfigureAwait(false);
        await _teamRepository.AddMemberAsync(team.Id, request.ManagerId, cancellationToken).ConfigureAwait(false);

        return await _teamRepository.GetResponseByIdAsync(team.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Team with ID '{team.Id}' was not found.");
    }

    public async Task<IReadOnlyList<TeamResponse>> GetTeamsAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserIdOrThrow();

        if (_currentUserService.IsInRole(UserRole.Admin.ToString()))
        {
            return await _teamRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        }

        if (_currentUserService.IsInRole(UserRole.Manager.ToString()))
        {
            return await _teamRepository.GetByManagerIdAsync(currentUserId, cancellationToken).ConfigureAwait(false);
        }

        var userTeam = await _teamRepository.GetByUserIdAsync(currentUserId, cancellationToken).ConfigureAwait(false);
        return userTeam is not null ? [userTeam] : [];
    }

    public async Task<TeamDetailsResponse> GetTeamByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserIdOrThrow();
        var teamDetails = await _teamRepository.GetDetailsByIdAsync(id, cancellationToken).ConfigureAwait(false);

        if (teamDetails is null)
        {
            throw new NotFoundException($"Team with ID '{id}' was not found.");
        }

        AuthorizeViewTeam(teamDetails, currentUserId);

        return teamDetails;
    }

    public async Task<TeamResponse> UpdateTeamAsync(
        Guid id,
        UpdateTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_currentUserService.IsInRole(UserRole.Admin.ToString()))
        {
            throw new ForbiddenException("Only Admins can update teams.");
        }

        var team = await _teamRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (team is null)
        {
            throw new NotFoundException($"Team with ID '{id}' was not found.");
        }

        await ApplyTeamUpdatesAsync(team, request, cancellationToken).ConfigureAwait(false);

        team.UpdatedAt = DateTime.UtcNow;
        await _teamRepository.UpdateAsync(team, cancellationToken).ConfigureAwait(false);

        return await _teamRepository.GetResponseByIdAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Team with ID '{id}' was not found.");
    }

    public async Task<TeamDetailsResponse> AddMemberAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var team = await _teamRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (team is null)
        {
            throw new NotFoundException($"Team with ID '{id}' was not found.");
        }

        AuthorizeTeamManagement(team);

        if (!await _teamRepository.UserExistsAsync(userId, cancellationToken).ConfigureAwait(false))
        {
            throw new NotFoundException($"User with ID '{userId}' was not found.");
        }

        var currentTeamId = await _teamRepository.GetUserTeamIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (currentTeamId == id)
        {
            throw new ConflictException("User is already a member of this team.");
        }

        if (currentTeamId.HasValue && currentTeamId.Value != id)
        {
            throw new ConflictException("User is already assigned to another team.");
        }

        await _teamRepository.AddMemberAsync(id, userId, cancellationToken).ConfigureAwait(false);

        return await _teamRepository.GetDetailsByIdAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Team with ID '{id}' was not found.");
    }

    public async Task<TeamDetailsResponse> RemoveMemberAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var team = await _teamRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (team is null)
        {
            throw new NotFoundException($"Team with ID '{id}' was not found.");
        }

        AuthorizeTeamManagement(team);

        if (!await _teamRepository.UserExistsAsync(userId, cancellationToken).ConfigureAwait(false))
        {
            throw new NotFoundException($"User with ID '{userId}' was not found.");
        }

        var currentTeamId = await _teamRepository.GetUserTeamIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (currentTeamId != id)
        {
            throw new ValidationException("User is not a member of this team.");
        }

        await _teamRepository.RemoveMemberAsync(id, userId, cancellationToken).ConfigureAwait(false);

        return await _teamRepository.GetDetailsByIdAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Team with ID '{id}' was not found.");
    }

    private Guid GetCurrentUserIdOrThrow()
    {
        return _currentUserService.UserId ?? throw new AuthException("User is not authenticated.");
    }

    private void AuthorizeViewTeam(TeamDetailsResponse team, Guid currentUserId)
    {
        if (_currentUserService.IsInRole(UserRole.Admin.ToString()))
        {
            return;
        }

        if (_currentUserService.IsInRole(UserRole.Manager.ToString()))
        {
            if (team.ManagerId == currentUserId || team.Members.Any(m => m.Id == currentUserId))
            {
                return;
            }

            throw new ForbiddenException("Managers can only view their own managed team.");
        }

        if (team.Members.Any(m => m.Id == currentUserId))
        {
            return;
        }

        throw new ForbiddenException("Users can only view their own assigned team.");
    }

    private void AuthorizeTeamManagement(Team team)
    {
        if (_currentUserService.IsInRole(UserRole.Admin.ToString()))
        {
            return;
        }

        var currentUserId = GetCurrentUserIdOrThrow();
        if (_currentUserService.IsInRole(UserRole.Manager.ToString()))
        {
            if (team.ManagerId == currentUserId)
            {
                return;
            }

            throw new ForbiddenException("Managers can only manage members of their own team.");
        }

        throw new ForbiddenException("Users cannot manage team membership.");
    }

    private async Task ValidateManagerAsync(Guid managerId, CancellationToken cancellationToken)
    {
        if (!await _teamRepository.UserExistsAsync(managerId, cancellationToken).ConfigureAwait(false))
        {
            throw new NotFoundException($"Manager with ID '{managerId}' was not found.");
        }

        if (!await _teamRepository.IsUserInRoleAsync(managerId, UserRole.Manager.ToString(), cancellationToken).ConfigureAwait(false))
        {
            throw new ValidationException("The assigned manager must have the Manager role.");
        }
    }

    private async Task ApplyTeamUpdatesAsync(Team team, UpdateTeamRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            ValidateTeamName(request.Name);
            if (await _teamRepository.NameExistsAsync(request.Name, team.Id, cancellationToken).ConfigureAwait(false))
            {
                throw new ConflictException($"A team with the name '{request.Name}' already exists.");
            }

            team.Name = request.Name.Trim();
        }

        if (request.ManagerId.HasValue && request.ManagerId.Value != Guid.Empty && request.ManagerId.Value != team.ManagerId)
        {
            await ValidateManagerAsync(request.ManagerId.Value, cancellationToken).ConfigureAwait(false);
            team.ManagerId = request.ManagerId.Value;
            await _teamRepository.AddMemberAsync(team.Id, request.ManagerId.Value, cancellationToken).ConfigureAwait(false);
        }
    }

    private static void ValidateTeamName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Team name is required.");
        }

        if (name.Trim().Length > 100)
        {
            throw new ValidationException("Team name cannot exceed 100 characters.");
        }
    }
}

