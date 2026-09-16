using TaskManagement.Application.DTOs.Teams;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using Xunit;

namespace TaskManagement.UnitTests;

public sealed class TeamServiceTests
{
    private readonly TestTeamRepository _teamRepository = new();
    private readonly TestCurrentUserService _currentUserService = new();
    private readonly TeamService _teamService;

    public TeamServiceTests()
    {
        _teamService = new TeamService(_teamRepository, _currentUserService);
    }

    [Fact]
    public async Task CreateTeamAsync_AsAdmin_WithValidData_Succeeds()
    {
        var adminId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        _currentUserService.SetUser(adminId, "admin@test.com", UserRole.Admin.ToString());
        _teamRepository.ExistingUsers[managerId] = UserRole.Manager.ToString();

        var request = new CreateTeamRequest
        {
            Name = "Frontend Team",
            ManagerId = managerId
        };

        var result = await _teamService.CreateTeamAsync(request);

        Assert.NotNull(result);
        Assert.Equal("Frontend Team", result.Name);
        Assert.Equal(managerId, result.ManagerId);
        Assert.True(_teamRepository.TeamMemberMap.ContainsKey(result.Id));
        Assert.Contains(managerId, _teamRepository.TeamMemberMap[result.Id]);
    }

    [Fact]
    public async Task CreateTeamAsync_AsManager_ThrowsForbidden()
    {
        var managerId = Guid.NewGuid();
        _currentUserService.SetUser(managerId, "mgr@test.com", UserRole.Manager.ToString());

        var request = new CreateTeamRequest
        {
            Name = "Frontend Team",
            ManagerId = managerId
        };

        await Assert.ThrowsAsync<ForbiddenException>(() => _teamService.CreateTeamAsync(request));
    }

    [Fact]
    public async Task CreateTeamAsync_WithDuplicateName_ThrowsConflict()
    {
        var adminId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        _currentUserService.SetUser(adminId, "admin@test.com", UserRole.Admin.ToString());
        _teamRepository.ExistingUsers[managerId] = UserRole.Manager.ToString();
        _teamRepository.ExistingNames.Add("Frontend Team");

        var request = new CreateTeamRequest
        {
            Name = "Frontend Team",
            ManagerId = managerId
        };

        await Assert.ThrowsAsync<ConflictException>(() => _teamService.CreateTeamAsync(request));
    }

    [Fact]
    public async Task CreateTeamAsync_WithCaseInsensitiveDuplicateName_ThrowsConflict()
    {
        var adminId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        _currentUserService.SetUser(adminId, "admin@test.com", UserRole.Admin.ToString());
        _teamRepository.ExistingUsers[managerId] = UserRole.Manager.ToString();
        _teamRepository.ExistingNames.Add("Development Team");

        var request = new CreateTeamRequest
        {
            Name = "development team",
            ManagerId = managerId
        };

        await Assert.ThrowsAsync<ConflictException>(() => _teamService.CreateTeamAsync(request));
    }

    [Fact]
    public async Task CreateTeamAsync_WithNonExistentManager_ThrowsNotFound()
    {
        var adminId = Guid.NewGuid();
        _currentUserService.SetUser(adminId, "admin@test.com", UserRole.Admin.ToString());

        var request = new CreateTeamRequest
        {
            Name = "Backend Team",
            ManagerId = Guid.NewGuid()
        };

        await Assert.ThrowsAsync<NotFoundException>(() => _teamService.CreateTeamAsync(request));
    }

    [Fact]
    public async Task CreateTeamAsync_WithUserWithoutManagerRole_ThrowsValidation()
    {
        var adminId = Guid.NewGuid();
        var normalUserId = Guid.NewGuid();
        _currentUserService.SetUser(adminId, "admin@test.com", UserRole.Admin.ToString());
        _teamRepository.ExistingUsers[normalUserId] = UserRole.User.ToString();

        var request = new CreateTeamRequest
        {
            Name = "Backend Team",
            ManagerId = normalUserId
        };

        await Assert.ThrowsAsync<ValidationException>(() => _teamService.CreateTeamAsync(request));
    }

    [Fact]
    public async Task CreateTeamAsync_WithEmptyName_ThrowsValidation()
    {
        var adminId = Guid.NewGuid();
        _currentUserService.SetUser(adminId, "admin@test.com", UserRole.Admin.ToString());

        var request = new CreateTeamRequest
        {
            Name = "   ",
            ManagerId = Guid.NewGuid()
        };

        await Assert.ThrowsAsync<ValidationException>(() => _teamService.CreateTeamAsync(request));
    }

    [Fact]
    public async Task GetTeamsAsync_AsAdmin_ReturnsAllTeams()
    {
        var adminId = Guid.NewGuid();
        _currentUserService.SetUser(adminId, "admin@test.com", UserRole.Admin.ToString());

        var t1 = new TeamResponse { Id = Guid.NewGuid(), Name = "T1" };
        var t2 = new TeamResponse { Id = Guid.NewGuid(), Name = "T2" };
        _teamRepository.TeamsList.AddRange([t1, t2]);

        var result = await _teamService.GetTeamsAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetTeamsAsync_AsManager_ReturnsOnlyManagedTeams()
    {
        var managerId = Guid.NewGuid();
        _currentUserService.SetUser(managerId, "mgr@test.com", UserRole.Manager.ToString());

        var t1 = new TeamResponse { Id = Guid.NewGuid(), Name = "T1", ManagerId = managerId };
        _teamRepository.ManagedTeamsList.Add(t1);

        var result = await _teamService.GetTeamsAsync();

        Assert.Single(result);
        Assert.Equal("T1", result[0].Name);
    }

    [Fact]
    public async Task GetTeamsAsync_WhenUnauthenticated_ThrowsAuthException()
    {
        _currentUserService.ClearUser();

        await Assert.ThrowsAsync<AuthException>(() => _teamService.GetTeamsAsync());
    }

    [Fact]
    public async Task GetTeamsAsync_AsUser_ReturnsOnlyAssignedTeam()
    {
        var userId = Guid.NewGuid();
        _currentUserService.SetUser(userId, "user@test.com", UserRole.User.ToString());

        var userTeam = new TeamResponse { Id = Guid.NewGuid(), Name = "User Team" };
        _teamRepository.UserTeamResponse = userTeam;

        var result = await _teamService.GetTeamsAsync();

        Assert.Single(result);
        Assert.Equal(userTeam.Id, result[0].Id);
    }

    [Fact]
    public async Task GetTeamByIdAsync_AsAdmin_ReturnsDetails()
    {
        var adminId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        _currentUserService.SetUser(adminId, "admin@test.com", UserRole.Admin.ToString());

        var details = new TeamDetailsResponse
        {
            Id = teamId,
            Name = "Core Team",
            ManagerId = Guid.NewGuid(),
            Members = [new TeamMemberResponse { Id = Guid.NewGuid(), Name = "Member 1" }]
        };
        _teamRepository.DetailsMap[teamId] = details;

        var result = await _teamService.GetTeamByIdAsync(teamId);

        Assert.NotNull(result);
        Assert.Equal(teamId, result.Id);
    }

    [Fact]
    public async Task GetTeamByIdAsync_AsManagerOfOtherTeam_ThrowsForbidden()
    {
        var managerId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        _currentUserService.SetUser(managerId, "mgr@test.com", UserRole.Manager.ToString());

        var details = new TeamDetailsResponse
        {
            Id = teamId,
            Name = "Other Team",
            ManagerId = Guid.NewGuid(),
            Members = []
        };
        _teamRepository.DetailsMap[teamId] = details;

        await Assert.ThrowsAsync<ForbiddenException>(() => _teamService.GetTeamByIdAsync(teamId));
    }

    [Fact]
    public async Task GetTeamByIdAsync_AsUserInTeam_ReturnsDetails()
    {
        var userId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        _currentUserService.SetUser(userId, "dev@test.com", UserRole.User.ToString());

        var details = new TeamDetailsResponse
        {
            Id = teamId,
            Name = "Dev Team",
            ManagerId = Guid.NewGuid(),
            Members = [new TeamMemberResponse { Id = userId, Name = "Dev User" }]
        };
        _teamRepository.DetailsMap[teamId] = details;

        var result = await _teamService.GetTeamByIdAsync(teamId);

        Assert.NotNull(result);
        Assert.Equal(teamId, result.Id);
    }

    [Fact]
    public async Task UpdateTeamAsync_AsAdmin_Succeeds()
    {
        var adminId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var newManagerId = Guid.NewGuid();
        _currentUserService.SetUser(adminId, "admin@test.com", UserRole.Admin.ToString());

        var team = new Team { Id = teamId, Name = "Old Name", ManagerId = Guid.NewGuid() };
        _teamRepository.TeamsMap[teamId] = team;
        _teamRepository.ExistingUsers[newManagerId] = UserRole.Manager.ToString();

        var request = new UpdateTeamRequest
        {
            Name = "New Name",
            ManagerId = newManagerId
        };

        var result = await _teamService.UpdateTeamAsync(teamId, request);

        Assert.NotNull(result);
        Assert.Equal("New Name", team.Name);
        Assert.Equal(newManagerId, team.ManagerId);
    }

    [Fact]
    public async Task UpdateTeamAsync_WithSameNameForSelf_Succeeds()
    {
        var adminId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        _currentUserService.SetUser(adminId, "admin@test.com", UserRole.Admin.ToString());

        var team = new Team { Id = teamId, Name = "Development Team", ManagerId = Guid.NewGuid() };
        _teamRepository.TeamsMap[teamId] = team;

        var request = new UpdateTeamRequest
        {
            Name = "development team"
        };

        var result = await _teamService.UpdateTeamAsync(teamId, request);

        Assert.NotNull(result);
        Assert.Equal("development team", team.Name);
    }

    [Fact]
    public async Task UpdateTeamAsync_WithCaseInsensitiveDuplicateOfOtherTeam_ThrowsConflict()
    {
        var adminId = Guid.NewGuid();
        var team1Id = Guid.NewGuid();
        var team2Id = Guid.NewGuid();
        _currentUserService.SetUser(adminId, "admin@test.com", UserRole.Admin.ToString());

        var team1 = new Team { Id = team1Id, Name = "Development Team", ManagerId = Guid.NewGuid() };
        var team2 = new Team { Id = team2Id, Name = "QA Team", ManagerId = Guid.NewGuid() };
        _teamRepository.TeamsMap[team1Id] = team1;
        _teamRepository.TeamsMap[team2Id] = team2;

        var request = new UpdateTeamRequest
        {
            Name = "development team"
        };

        await Assert.ThrowsAsync<ConflictException>(() => _teamService.UpdateTeamAsync(team2Id, request));
    }

    [Fact]
    public async Task AddMemberAsync_AsManagerOfTeam_Succeeds()
    {
        var managerId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _currentUserService.SetUser(managerId, "mgr@test.com", UserRole.Manager.ToString());

        var team = new Team { Id = teamId, Name = "My Team", ManagerId = managerId };
        _teamRepository.TeamsMap[teamId] = team;
        _teamRepository.ExistingUsers[userId] = UserRole.User.ToString();

        var result = await _teamService.AddMemberAsync(teamId, userId);

        Assert.NotNull(result);
        Assert.True(_teamRepository.TeamMemberMap.ContainsKey(teamId));
        Assert.Contains(userId, _teamRepository.TeamMemberMap[teamId]);
    }

    [Fact]
    public async Task AddMemberAsync_WhenUserAlreadyInAnotherTeam_ThrowsConflict()
    {
        var managerId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var otherTeamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _currentUserService.SetUser(managerId, "mgr@test.com", UserRole.Manager.ToString());

        var team = new Team { Id = teamId, Name = "My Team", ManagerId = managerId };
        _teamRepository.TeamsMap[teamId] = team;
        _teamRepository.ExistingUsers[userId] = UserRole.User.ToString();
        _teamRepository.UserTeamMap[userId] = otherTeamId;

        await Assert.ThrowsAsync<ConflictException>(() => _teamService.AddMemberAsync(teamId, userId));
    }

    [Fact]
    public async Task RemoveMemberAsync_AsManagerOfTeam_Succeeds()
    {
        var managerId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _currentUserService.SetUser(managerId, "mgr@test.com", UserRole.Manager.ToString());

        var team = new Team { Id = teamId, Name = "My Team", ManagerId = managerId };
        _teamRepository.TeamsMap[teamId] = team;
        _teamRepository.ExistingUsers[userId] = UserRole.User.ToString();
        _teamRepository.UserTeamMap[userId] = teamId;

        var result = await _teamService.RemoveMemberAsync(teamId, userId);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task RemoveMemberAsync_WhenUserNotMemberOfTeam_ThrowsValidation()
    {
        var managerId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var otherTeamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _currentUserService.SetUser(managerId, "mgr@test.com", UserRole.Manager.ToString());

        var team = new Team { Id = teamId, Name = "My Team", ManagerId = managerId };
        _teamRepository.TeamsMap[teamId] = team;
        _teamRepository.ExistingUsers[userId] = UserRole.User.ToString();
        _teamRepository.UserTeamMap[userId] = otherTeamId;

        await Assert.ThrowsAsync<ValidationException>(() => _teamService.RemoveMemberAsync(teamId, userId));
    }

    private sealed class TestTeamRepository : ITeamRepository
    {
        public Dictionary<Guid, Team> TeamsMap { get; } = new();
        public Dictionary<Guid, TeamDetailsResponse> DetailsMap { get; } = new();
        public List<TeamResponse> TeamsList { get; } = [];
        public List<TeamResponse> ManagedTeamsList { get; } = [];
        public TeamResponse? UserTeamResponse { get; set; }
        public HashSet<string> ExistingNames { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<Guid, string> ExistingUsers { get; } = new();
        public Dictionary<Guid, Guid> UserTeamMap { get; } = new();
        public Dictionary<Guid, List<Guid>> TeamMemberMap { get; } = new();

        public Task<Team?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            TeamsMap.TryGetValue(id, out var team);
            return Task.FromResult(team);
        }

        public Task<TeamResponse?> GetResponseByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (TeamsMap.TryGetValue(id, out var team))
            {
                return Task.FromResult<TeamResponse?>(new TeamResponse
                {
                    Id = team.Id,
                    Name = team.Name,
                    ManagerId = team.ManagerId
                });
            }
            return Task.FromResult<TeamResponse?>(null);
        }

        public Task<TeamDetailsResponse?> GetDetailsByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (DetailsMap.TryGetValue(id, out var details))
            {
                return Task.FromResult<TeamDetailsResponse?>(details);
            }
            if (TeamsMap.TryGetValue(id, out var team))
            {
                return Task.FromResult<TeamDetailsResponse?>(new TeamDetailsResponse
                {
                    Id = team.Id,
                    Name = team.Name,
                    ManagerId = team.ManagerId
                });
            }
            return Task.FromResult<TeamDetailsResponse?>(null);
        }

        public Task<IReadOnlyList<TeamResponse>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TeamResponse>>(TeamsList);
        }

        public Task<IReadOnlyList<TeamResponse>> GetByManagerIdAsync(Guid managerId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TeamResponse>>(ManagedTeamsList);
        }

        public Task<TeamResponse?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(UserTeamResponse);
        }

        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(TeamsMap.ContainsKey(id));
        }

        public Task<bool> NameExistsAsync(string name, Guid? excludeTeamId = null, CancellationToken cancellationToken = default)
        {
            if (TeamsMap.Count > 0)
            {
                var query = TeamsMap.Values.AsEnumerable();
                if (excludeTeamId.HasValue)
                {
                    query = query.Where(t => t.Id != excludeTeamId.Value);
                }
                if (query.Any(t => string.Equals(t.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(ExistingNames.Contains(name.Trim()));
        }

        public Task<Team> CreateAsync(Team team, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(team);
            TeamsMap[team.Id] = team;
            ExistingNames.Add(team.Name);
            return Task.FromResult(team);
        }

        public Task<bool> UpdateAsync(Team team, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(team);
            TeamsMap[team.Id] = team;
            return Task.FromResult(true);
        }

        public Task<bool> AddMemberAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default)
        {
            if (!TeamMemberMap.TryGetValue(teamId, out var list))
            {
                list = [];
                TeamMemberMap[teamId] = list;
            }
            list.Add(userId);
            UserTeamMap[userId] = teamId;
            return Task.FromResult(true);
        }

        public Task<bool> RemoveMemberAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default)
        {
            if (TeamMemberMap.TryGetValue(teamId, out var list))
            {
                list.Remove(userId);
            }
            UserTeamMap.Remove(userId);
            return Task.FromResult(true);
        }

        public Task<bool> IsUserInRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default)
        {
            if (ExistingUsers.TryGetValue(userId, out var userRole))
            {
                return Task.FromResult(string.Equals(userRole, role, StringComparison.OrdinalIgnoreCase));
            }
            return Task.FromResult(false);
        }

        public Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ExistingUsers.ContainsKey(userId));
        }

        public Task<Guid?> GetUserTeamIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            if (UserTeamMap.TryGetValue(userId, out var teamId))
            {
                return Task.FromResult<Guid?>(teamId);
            }
            return Task.FromResult<Guid?>(null);
        }
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public Guid? UserId { get; private set; }
        public string? Email { get; private set; }
        public string? Role { get; private set; }
        public bool IsAuthenticated => UserId.HasValue;

        public void SetUser(Guid userId, string email, string role)
        {
            UserId = userId;
            Email = email;
            Role = role;
        }

        public void ClearUser()
        {
            UserId = null;
            Email = null;
            Role = null;
        }

        public bool IsInRole(string role)
        {
            return string.Equals(Role, role, StringComparison.OrdinalIgnoreCase);
        }
    }
}
