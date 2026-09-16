using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs.Users;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Enums;
using Xunit;

namespace TaskManagement.UnitTests;

public sealed class UserServiceTests
{
    private readonly TestUserRepository _userRepository = new();
    private readonly TestCurrentUserService _currentUserService = new();
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _userService = new UserService(_userRepository, _currentUserService);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ReturnsUser_WhenAuthenticated()
    {
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId, "Admin User", "admin@test.com", UserRole.Admin.ToString(), null);
        _userRepository.Users[userId] = user;
        _currentUserService.SetUser(userId, "admin@test.com", UserRole.Admin.ToString());

        var result = await _userService.GetCurrentUserAsync();

        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("Admin User", result.Name);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ThrowsAuthException_WhenUnauthenticated()
    {
        _currentUserService.ClearUser();

        await Assert.ThrowsAsync<AuthException>(() => _userService.GetCurrentUserAsync());
    }

    [Fact]
    public async Task GetCurrentUserAsync_ThrowsNotFoundException_WhenUserNotFound()
    {
        var userId = Guid.NewGuid();
        _currentUserService.SetUser(userId, "unknown@test.com", UserRole.User.ToString());

        await Assert.ThrowsAsync<NotFoundException>(() => _userService.GetCurrentUserAsync());
    }

    [Fact]
    public async Task GetUsersAsync_AsAdmin_ReturnsAllUsers()
    {
        var adminId = Guid.NewGuid();
        _currentUserService.SetUser(adminId, "admin@test.com", UserRole.Admin.ToString());

        var u1 = CreateTestUser(Guid.NewGuid(), "U1", "u1@test.com", UserRole.User.ToString(), Guid.NewGuid());
        var u2 = CreateTestUser(Guid.NewGuid(), "U2", "u2@test.com", UserRole.User.ToString(), Guid.NewGuid());
        _userRepository.Users[u1.Id] = u1;
        _userRepository.Users[u2.Id] = u2;

        var result = await _userService.GetUsersAsync(new UserQueryParameters { Page = 1, PageSize = 10 });

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetUsersAsync_AsManager_ScopesToOwnTeam()
    {
        var managerTeamId = Guid.NewGuid();
        var otherTeamId = Guid.NewGuid();
        var managerId = Guid.NewGuid();

        _currentUserService.SetUser(managerId, "mgr@test.com", UserRole.Manager.ToString());
        _userRepository.UserTeamMap[managerId] = managerTeamId;

        var u1 = CreateTestUser(Guid.NewGuid(), "Team Member", "tm@test.com", UserRole.User.ToString(), managerTeamId);
        var u2 = CreateTestUser(Guid.NewGuid(), "Other Member", "om@test.com", UserRole.User.ToString(), otherTeamId);
        _userRepository.Users[u1.Id] = u1;
        _userRepository.Users[u2.Id] = u2;

        var result = await _userService.GetUsersAsync(new UserQueryParameters { Page = 1, PageSize = 10 });

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(u1.Id, result.Items[0].Id);
    }

    [Fact]
    public async Task GetUsersAsync_AsManager_ThrowsForbidden_WhenFilteringOtherTeam()
    {
        var managerTeamId = Guid.NewGuid();
        var otherTeamId = Guid.NewGuid();
        var managerId = Guid.NewGuid();

        _currentUserService.SetUser(managerId, "mgr@test.com", UserRole.Manager.ToString());
        _userRepository.UserTeamMap[managerId] = managerTeamId;

        var query = new UserQueryParameters { TeamId = otherTeamId };

        await Assert.ThrowsAsync<ForbiddenException>(() => _userService.GetUsersAsync(query));
    }

    [Fact]
    public async Task GetUsersAsync_AsUser_ThrowsForbiddenException()
    {
        var userId = Guid.NewGuid();
        _currentUserService.SetUser(userId, "user@test.com", UserRole.User.ToString());

        await Assert.ThrowsAsync<ForbiddenException>(() => _userService.GetUsersAsync(new UserQueryParameters()));
    }

    [Fact]
    public async Task GetUserByIdAsync_AsAdmin_CanViewAnyUser()
    {
        var adminId = Guid.NewGuid();
        _currentUserService.SetUser(adminId, "admin@test.com", UserRole.Admin.ToString());

        var targetId = Guid.NewGuid();
        var targetUser = CreateTestUser(targetId, "Target", "target@test.com", UserRole.User.ToString(), Guid.NewGuid());
        _userRepository.Users[targetId] = targetUser;

        var result = await _userService.GetUserByIdAsync(targetId);

        Assert.NotNull(result);
        Assert.Equal(targetId, result.Id);
    }

    [Fact]
    public async Task GetUserByIdAsync_AsManager_CanViewSameTeamMember()
    {
        var teamId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        _currentUserService.SetUser(managerId, "mgr@test.com", UserRole.Manager.ToString());
        _userRepository.UserTeamMap[managerId] = teamId;

        var memberUser = CreateTestUser(memberId, "Member", "member@test.com", UserRole.User.ToString(), teamId);
        _userRepository.Users[memberId] = memberUser;

        var result = await _userService.GetUserByIdAsync(memberId);

        Assert.NotNull(result);
        Assert.Equal(memberId, result.Id);
    }

    [Fact]
    public async Task GetUserByIdAsync_AsManager_ThrowsForbidden_WhenViewingDifferentTeamUser()
    {
        var managerTeamId = Guid.NewGuid();
        var otherTeamId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        _currentUserService.SetUser(managerId, "mgr@test.com", UserRole.Manager.ToString());
        _userRepository.UserTeamMap[managerId] = managerTeamId;

        var otherUser = CreateTestUser(otherUserId, "Other", "other@test.com", UserRole.User.ToString(), otherTeamId);
        _userRepository.Users[otherUserId] = otherUser;

        await Assert.ThrowsAsync<ForbiddenException>(() => _userService.GetUserByIdAsync(otherUserId));
    }

    [Fact]
    public async Task GetUserByIdAsync_AsUser_CanViewSelf()
    {
        var userId = Guid.NewGuid();
        _currentUserService.SetUser(userId, "user@test.com", UserRole.User.ToString());

        var user = CreateTestUser(userId, "Self", "user@test.com", UserRole.User.ToString(), null);
        _userRepository.Users[userId] = user;

        var result = await _userService.GetUserByIdAsync(userId);

        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
    }

    [Fact]
    public async Task GetUserByIdAsync_AsUser_ThrowsForbidden_WhenViewingAnotherUser()
    {
        var userId = Guid.NewGuid();
        var otherId = Guid.NewGuid();

        _currentUserService.SetUser(userId, "user@test.com", UserRole.User.ToString());

        var otherUser = CreateTestUser(otherId, "Other", "other@test.com", UserRole.User.ToString(), null);
        _userRepository.Users[otherId] = otherUser;

        await Assert.ThrowsAsync<ForbiddenException>(() => _userService.GetUserByIdAsync(otherId));
    }

    [Fact]
    public async Task GetUserByIdAsync_ThrowsNotFoundException_WhenUserDoesNotExist()
    {
        var adminId = Guid.NewGuid();
        _currentUserService.SetUser(adminId, "admin@test.com", UserRole.Admin.ToString());

        await Assert.ThrowsAsync<NotFoundException>(() => _userService.GetUserByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateUserRoleAsync_AsAdmin_WithValidRole_UpdatesRole()
    {
        var adminId = Guid.NewGuid();
        _currentUserService.SetUser(adminId, "admin@test.com", UserRole.Admin.ToString());

        var targetId = Guid.NewGuid();
        var user = CreateTestUser(targetId, "User", "user@test.com", UserRole.User.ToString(), null);
        _userRepository.Users[targetId] = user;

        var response = await _userService.UpdateUserRoleAsync(targetId, new ChangeUserRoleRequest { Role = "Manager" });

        Assert.NotNull(response);
        Assert.Equal(UserRole.Manager.ToString(), response.Role);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_AsAdmin_WithInvalidRole_ThrowsValidationException()
    {
        var adminId = Guid.NewGuid();
        _currentUserService.SetUser(adminId, "admin@test.com", UserRole.Admin.ToString());

        var targetId = Guid.NewGuid();
        var user = CreateTestUser(targetId, "User", "user@test.com", UserRole.User.ToString(), null);
        _userRepository.Users[targetId] = user;

        await Assert.ThrowsAsync<ValidationException>(() =>
            _userService.UpdateUserRoleAsync(targetId, new ChangeUserRoleRequest { Role = "SuperHero" }));
    }

    [Fact]
    public async Task UpdateUserRoleAsync_AsNonAdmin_ThrowsForbiddenException()
    {
        var userId = Guid.NewGuid();
        _currentUserService.SetUser(userId, "user@test.com", UserRole.User.ToString());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _userService.UpdateUserRoleAsync(userId, new ChangeUserRoleRequest { Role = "Admin" }));
    }

    [Fact]
    public async Task UpdateUserTeamAsync_AsAdmin_WithValidTeam_UpdatesTeam()
    {
        var adminId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var teamId = Guid.NewGuid();

        _currentUserService.SetUser(adminId, "admin@test.com", UserRole.Admin.ToString());
        var user = CreateTestUser(targetId, "User", "user@test.com", UserRole.User.ToString(), null);
        _userRepository.Users[targetId] = user;
        _userRepository.ExistingTeams.Add(teamId);

        var response = await _userService.UpdateUserTeamAsync(targetId, new AssignUserTeamRequest { TeamId = teamId });

        Assert.NotNull(response);
        Assert.Equal(teamId, response.TeamId);
    }

    [Fact]
    public async Task UpdateUserTeamAsync_AsAdmin_WithNonExistentTeam_ThrowsNotFoundException()
    {
        var adminId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var nonExistentTeamId = Guid.NewGuid();

        _currentUserService.SetUser(adminId, "admin@test.com", UserRole.Admin.ToString());
        var user = CreateTestUser(targetId, "User", "user@test.com", UserRole.User.ToString(), null);
        _userRepository.Users[targetId] = user;

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _userService.UpdateUserTeamAsync(targetId, new AssignUserTeamRequest { TeamId = nonExistentTeamId }));
    }

    [Fact]
    public async Task UpdateUserTeamAsync_AsNonAdmin_ThrowsForbiddenException()
    {
        var userId = Guid.NewGuid();
        _currentUserService.SetUser(userId, "user@test.com", UserRole.User.ToString());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _userService.UpdateUserTeamAsync(userId, new AssignUserTeamRequest { TeamId = Guid.NewGuid() }));
    }

    private static UserResponse CreateTestUser(Guid id, string name, string email, string role, Guid? teamId)
    {
        return new UserResponse
        {
            Id = id,
            Name = name,
            Email = email,
            Role = role,
            TeamId = teamId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private sealed class TestUserRepository : IUserRepository
    {
        public Dictionary<Guid, UserResponse> Users { get; } = [];
        public Dictionary<Guid, Guid?> UserTeamMap { get; } = [];
        public HashSet<Guid> ExistingTeams { get; } = [];

        public Task<UserResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Users.TryGetValue(id, out var user);
            return Task.FromResult(user);
        }

        public Task<UserResponse?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var user = Users.Values.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(user);
        }

        public Task<PagedResult<UserResponse>> GetPagedAsync(
            int page,
            int pageSize,
            Guid? teamId = null,
            string? role = null,
            CancellationToken cancellationToken = default)
        {
            var query = Users.Values.AsEnumerable();
            if (teamId.HasValue)
            {
                query = query.Where(u => u.TeamId == teamId.Value);
            }
            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(u => string.Equals(u.Role, role, StringComparison.OrdinalIgnoreCase));
            }

            var list = query.ToList();
            var paged = new PagedResult<UserResponse>
            {
                Items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = list.Count
            };
            return Task.FromResult(paged);
        }

        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Users.ContainsKey(id));
        }

        public Task<bool> TeamExistsAsync(Guid teamId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ExistingTeams.Contains(teamId));
        }

        public Task<Guid?> GetUserTeamIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            if (UserTeamMap.TryGetValue(userId, out var teamId))
            {
                return Task.FromResult(teamId);
            }
            if (Users.TryGetValue(userId, out var user))
            {
                return Task.FromResult(user.TeamId);
            }
            return Task.FromResult<Guid?>(null);
        }

        public Task<bool> UpdateRoleAsync(Guid userId, string newRole, CancellationToken cancellationToken = default)
        {
            if (Users.TryGetValue(userId, out var user))
            {
                user.Role = newRole;
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<bool> UpdateTeamAsync(Guid userId, Guid? teamId, CancellationToken cancellationToken = default)
        {
            if (Users.TryGetValue(userId, out var user))
            {
                user.TeamId = teamId;
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
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

