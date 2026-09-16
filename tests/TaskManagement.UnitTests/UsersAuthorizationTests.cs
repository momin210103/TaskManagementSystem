using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Application.DTOs.Users;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Identity;
using TaskManagement.Infrastructure.Persistence;
using TaskManagement.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TaskManagement.UnitTests;

public sealed class UsersAuthorizationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly UserRepository _userRepository;
    private readonly TestCurrentUserService _currentUserService;
    private readonly UserService _userService;

    public UsersAuthorizationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var dbName = Guid.NewGuid().ToString();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        _userManager = _serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        _roleManager = _serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        _userRepository = new UserRepository(_dbContext, _userManager, _roleManager);
        _currentUserService = new TestCurrentUserService();
        _userService = new UserService(_userRepository, _currentUserService);

        // Seed initial data (Admin, Manager 1, Manager 2, Dev 1, Dev 2, QA 1, Teams, Tasks)
        DatabaseSeeder.SeedAsync(_serviceProvider).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task AnonymousUser_GetCurrentUser_ThrowsAuthException()
    {
        _currentUserService.ClearUser();

        await Assert.ThrowsAsync<AuthException>(() => _userService.GetCurrentUserAsync());
    }

    [Fact]
    public async Task NormalUser_GetCurrentUser_ReturnsOwnProfile()
    {
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        Assert.NotNull(dev1);
        _currentUserService.SetUser(dev1.Id, dev1.Email!, UserRole.User.ToString());

        var result = await _userService.GetCurrentUserAsync();

        Assert.NotNull(result);
        Assert.Equal(dev1.Id, result.Id);
        Assert.Equal("John Developer", result.Name);
        Assert.Equal("dev1@taskmanagement.com", result.Email);
        Assert.Equal(UserRole.User.ToString(), result.Role);
    }

    [Fact]
    public async Task NormalUser_GetUsers_ThrowsForbidden()
    {
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        Assert.NotNull(dev1);
        _currentUserService.SetUser(dev1.Id, dev1.Email!, UserRole.User.ToString());

        await Assert.ThrowsAsync<ForbiddenException>(() => _userService.GetUsersAsync(new UserQueryParameters()));
    }

    [Fact]
    public async Task Manager_GetUsers_ReturnsOnlyOwnTeamMembers()
    {
        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        Assert.NotNull(manager1);
        _currentUserService.SetUser(manager1.Id, manager1.Email!, UserRole.Manager.ToString());

        var result = await _userService.GetUsersAsync(new UserQueryParameters());

        Assert.NotNull(result);
        Assert.True(result.Items.Count > 0);
        // All returned members must belong to manager1's team
        Assert.All(result.Items, u => Assert.Equal(manager1.TeamId, u.TeamId));
    }

    [Fact]
    public async Task Manager_GetUsers_WithOtherTeamId_ThrowsForbidden()
    {
        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        Assert.NotNull(manager1);
        _currentUserService.SetUser(manager1.Id, manager1.Email!, UserRole.Manager.ToString());

        var otherTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "QA & Testing Team");
        Assert.NotNull(otherTeam);

        var query = new UserQueryParameters { TeamId = otherTeam.Id };

        await Assert.ThrowsAsync<ForbiddenException>(() => _userService.GetUsersAsync(query));
    }

    [Fact]
    public async Task Admin_GetUsers_ReturnsAllUsersAcrossTeams()
    {
        var admin = await _userManager.FindByEmailAsync("admin@taskmanagement.com");
        Assert.NotNull(admin);
        _currentUserService.SetUser(admin.Id, admin.Email!, UserRole.Admin.ToString());

        var result = await _userService.GetUsersAsync(new UserQueryParameters { Page = 1, PageSize = 20 });

        Assert.NotNull(result);
        Assert.Equal(6, result.TotalCount);
    }

    [Fact]
    public async Task NormalUser_GetUserById_ForSelf_Succeeds()
    {
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        Assert.NotNull(dev1);
        _currentUserService.SetUser(dev1.Id, dev1.Email!, UserRole.User.ToString());

        var result = await _userService.GetUserByIdAsync(dev1.Id);

        Assert.NotNull(result);
        Assert.Equal(dev1.Id, result.Id);
    }

    [Fact]
    public async Task NormalUser_GetUserById_ForOtherUser_ThrowsForbidden_IDOR()
    {
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        var dev2 = await _userManager.FindByEmailAsync("dev2@taskmanagement.com");
        Assert.NotNull(dev1);
        Assert.NotNull(dev2);

        _currentUserService.SetUser(dev1.Id, dev1.Email!, UserRole.User.ToString());

        await Assert.ThrowsAsync<ForbiddenException>(() => _userService.GetUserByIdAsync(dev2.Id));
    }

    [Fact]
    public async Task Manager_GetUserById_ForSameTeamMember_Succeeds()
    {
        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        Assert.NotNull(manager1);
        Assert.NotNull(dev1);

        _currentUserService.SetUser(manager1.Id, manager1.Email!, UserRole.Manager.ToString());

        var result = await _userService.GetUserByIdAsync(dev1.Id);

        Assert.NotNull(result);
        Assert.Equal(dev1.Id, result.Id);
    }

    [Fact]
    public async Task Manager_GetUserById_ForOtherTeamMember_ThrowsForbidden_IDOR()
    {
        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        var qa1 = await _userManager.FindByEmailAsync("qa1@taskmanagement.com");
        Assert.NotNull(manager1);
        Assert.NotNull(qa1);

        _currentUserService.SetUser(manager1.Id, manager1.Email!, UserRole.Manager.ToString());

        await Assert.ThrowsAsync<ForbiddenException>(() => _userService.GetUserByIdAsync(qa1.Id));
    }

    [Fact]
    public async Task Admin_GetUserById_ForAnyUser_Succeeds()
    {
        var admin = await _userManager.FindByEmailAsync("admin@taskmanagement.com");
        var qa1 = await _userManager.FindByEmailAsync("qa1@taskmanagement.com");
        Assert.NotNull(admin);
        Assert.NotNull(qa1);

        _currentUserService.SetUser(admin.Id, admin.Email!, UserRole.Admin.ToString());

        var result = await _userService.GetUserByIdAsync(qa1.Id);

        Assert.NotNull(result);
        Assert.Equal(qa1.Id, result.Id);
    }

    [Fact]
    public async Task NormalUser_UpdateRole_ThrowsForbidden_PrivilegeEscalation()
    {
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        Assert.NotNull(dev1);
        _currentUserService.SetUser(dev1.Id, dev1.Email!, UserRole.User.ToString());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _userService.UpdateUserRoleAsync(dev1.Id, new ChangeUserRoleRequest { Role = "Admin" }));
    }

    [Fact]
    public async Task Manager_UpdateRole_ThrowsForbidden_PrivilegeEscalation()
    {
        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        Assert.NotNull(manager1);
        _currentUserService.SetUser(manager1.Id, manager1.Email!, UserRole.Manager.ToString());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _userService.UpdateUserRoleAsync(manager1.Id, new ChangeUserRoleRequest { Role = "Admin" }));
    }

    [Fact]
    public async Task Admin_UpdateRole_WithValidRole_Succeeds()
    {
        var admin = await _userManager.FindByEmailAsync("admin@taskmanagement.com");
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        Assert.NotNull(admin);
        Assert.NotNull(dev1);

        _currentUserService.SetUser(admin.Id, admin.Email!, UserRole.Admin.ToString());

        var result = await _userService.UpdateUserRoleAsync(dev1.Id, new ChangeUserRoleRequest { Role = "Manager" });

        Assert.NotNull(result);
        Assert.Equal(UserRole.Manager.ToString(), result.Role);

        var updatedDev = await _userManager.FindByIdAsync(dev1.Id.ToString());
        Assert.NotNull(updatedDev);
        Assert.True(await _userManager.IsInRoleAsync(updatedDev, UserRole.Manager.ToString()));
    }

    [Fact]
    public async Task Admin_UpdateRole_WithInvalidRole_ThrowsValidationException()
    {
        var admin = await _userManager.FindByEmailAsync("admin@taskmanagement.com");
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        Assert.NotNull(admin);
        Assert.NotNull(dev1);

        _currentUserService.SetUser(admin.Id, admin.Email!, UserRole.Admin.ToString());

        await Assert.ThrowsAsync<ValidationException>(() =>
            _userService.UpdateUserRoleAsync(dev1.Id, new ChangeUserRoleRequest { Role = "Owner" }));
    }

    [Fact]
    public async Task Admin_UpdateRole_ForNonExistentUser_ThrowsNotFound()
    {
        var admin = await _userManager.FindByEmailAsync("admin@taskmanagement.com");
        Assert.NotNull(admin);
        _currentUserService.SetUser(admin.Id, admin.Email!, UserRole.Admin.ToString());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _userService.UpdateUserRoleAsync(Guid.NewGuid(), new ChangeUserRoleRequest { Role = "Manager" }));
    }

    [Fact]
    public async Task Admin_UpdateTeam_WithValidTeam_Succeeds()
    {
        var admin = await _userManager.FindByEmailAsync("admin@taskmanagement.com");
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        var qaTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "QA & Testing Team");
        Assert.NotNull(admin);
        Assert.NotNull(dev1);
        Assert.NotNull(qaTeam);

        _currentUserService.SetUser(admin.Id, admin.Email!, UserRole.Admin.ToString());

        var result = await _userService.UpdateUserTeamAsync(dev1.Id, new AssignUserTeamRequest { TeamId = qaTeam.Id });

        Assert.NotNull(result);
        Assert.Equal(qaTeam.Id, result.TeamId);
    }

    [Fact]
    public async Task Admin_UpdateTeam_WithNonExistentTeam_ThrowsNotFound()
    {
        var admin = await _userManager.FindByEmailAsync("admin@taskmanagement.com");
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        Assert.NotNull(admin);
        Assert.NotNull(dev1);

        _currentUserService.SetUser(admin.Id, admin.Email!, UserRole.Admin.ToString());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _userService.UpdateUserTeamAsync(dev1.Id, new AssignUserTeamRequest { TeamId = Guid.NewGuid() }));
    }

    [Fact]
    public async Task NormalUser_UpdateTeam_ThrowsForbidden()
    {
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        Assert.NotNull(dev1);
        _currentUserService.SetUser(dev1.Id, dev1.Email!, UserRole.User.ToString());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _userService.UpdateUserTeamAsync(dev1.Id, new AssignUserTeamRequest { TeamId = Guid.NewGuid() }));
    }

    [Fact]
    public async Task Manager_UpdateTeam_ThrowsForbidden()
    {
        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        Assert.NotNull(manager1);
        Assert.NotNull(dev1);

        _currentUserService.SetUser(manager1.Id, manager1.Email!, UserRole.Manager.ToString());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _userService.UpdateUserTeamAsync(dev1.Id, new AssignUserTeamRequest { TeamId = Guid.NewGuid() }));
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _userManager.Dispose();
        _roleManager.Dispose();
        _serviceProvider.Dispose();
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

