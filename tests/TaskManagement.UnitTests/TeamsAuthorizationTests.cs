using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Application.DTOs.Teams;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Identity;
using TaskManagement.Infrastructure.Persistence;
using TaskManagement.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TaskManagement.UnitTests;

public sealed class TeamsAuthorizationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly TeamRepository _teamRepository;
    private readonly TestCurrentUserService _currentUserService;
    private readonly TeamService _teamService;

    public TeamsAuthorizationTests()
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

        _teamRepository = new TeamRepository(_dbContext, _roleManager);
        _currentUserService = new TestCurrentUserService();
        _teamService = new TeamService(_teamRepository, _currentUserService);

        // Seed initial data (Admin, Manager 1, Manager 2, Dev 1, Dev 2, QA 1, Teams, Tasks)
        DatabaseSeeder.SeedAsync(_serviceProvider).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task AnonymousUser_GetTeams_ThrowsAuthException()
    {
        _currentUserService.ClearUser();

        await Assert.ThrowsAsync<AuthException>(() => _teamService.GetTeamsAsync());
    }

    [Fact]
    public async Task Admin_CreateTeam_Succeeds()
    {
        var admin = await _userManager.FindByEmailAsync("admin@taskmanagement.com");
        var manager2 = await _userManager.FindByEmailAsync("manager2@taskmanagement.com");
        Assert.NotNull(admin);
        Assert.NotNull(manager2);

        _currentUserService.SetUser(admin.Id, admin.Email!, UserRole.Admin.ToString());

        var result = await _teamService.CreateTeamAsync(new CreateTeamRequest
        {
            Name = "Mobile App Team",
            ManagerId = manager2.Id
        });

        Assert.NotNull(result);
        Assert.Equal("Mobile App Team", result.Name);
        Assert.Equal(manager2.Id, result.ManagerId);
    }

    [Fact]
    public async Task Manager_CreateTeam_ThrowsForbidden()
    {
        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        Assert.NotNull(manager1);
        _currentUserService.SetUser(manager1.Id, manager1.Email!, UserRole.Manager.ToString());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _teamService.CreateTeamAsync(new CreateTeamRequest
            {
                Name = "Unauthorized Team",
                ManagerId = manager1.Id
            }));
    }

    [Fact]
    public async Task User_CreateTeam_ThrowsForbidden()
    {
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        Assert.NotNull(dev1);
        _currentUserService.SetUser(dev1.Id, dev1.Email!, UserRole.User.ToString());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _teamService.CreateTeamAsync(new CreateTeamRequest
            {
                Name = "Unauthorized Team",
                ManagerId = dev1.Id
            }));
    }

    [Fact]
    public async Task Admin_GetTeams_ReturnsAllTeams()
    {
        var admin = await _userManager.FindByEmailAsync("admin@taskmanagement.com");
        Assert.NotNull(admin);
        _currentUserService.SetUser(admin.Id, admin.Email!, UserRole.Admin.ToString());

        var teams = await _teamService.GetTeamsAsync();

        Assert.NotNull(teams);
        Assert.Equal(2, teams.Count);
    }

    [Fact]
    public async Task Manager_GetTeams_ReturnsOnlyOwnManagedTeam()
    {
        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        Assert.NotNull(manager1);
        _currentUserService.SetUser(manager1.Id, manager1.Email!, UserRole.Manager.ToString());

        var teams = await _teamService.GetTeamsAsync();

        Assert.NotNull(teams);
        Assert.Single(teams);
        Assert.Equal("Engineering Team", teams[0].Name);
        Assert.Equal(manager1.Id, teams[0].ManagerId);
    }

    [Fact]
    public async Task User_GetTeams_ReturnsOnlyOwnAssignedTeam()
    {
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        Assert.NotNull(dev1);
        _currentUserService.SetUser(dev1.Id, dev1.Email!, UserRole.User.ToString());

        var teams = await _teamService.GetTeamsAsync();

        Assert.NotNull(teams);
        Assert.Single(teams);
        Assert.Equal("Engineering Team", teams[0].Name);
    }

    [Fact]
    public async Task Admin_GetTeamById_ReturnsAnyTeam()
    {
        var admin = await _userManager.FindByEmailAsync("admin@taskmanagement.com");
        var team = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "QA & Testing Team");
        Assert.NotNull(admin);
        Assert.NotNull(team);

        _currentUserService.SetUser(admin.Id, admin.Email!, UserRole.Admin.ToString());

        var result = await _teamService.GetTeamByIdAsync(team.Id);

        Assert.NotNull(result);
        Assert.Equal("QA & Testing Team", result.Name);
    }

    [Fact]
    public async Task Manager_GetTeamById_ForOwnTeam_Succeeds()
    {
        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        var engTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "Engineering Team");
        Assert.NotNull(manager1);
        Assert.NotNull(engTeam);

        _currentUserService.SetUser(manager1.Id, manager1.Email!, UserRole.Manager.ToString());

        var result = await _teamService.GetTeamByIdAsync(engTeam.Id);

        Assert.NotNull(result);
        Assert.Equal(engTeam.Id, result.Id);
    }

    [Fact]
    public async Task Manager_GetTeamById_ForOtherTeam_ThrowsForbidden_IDOR()
    {
        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        var qaTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "QA & Testing Team");
        Assert.NotNull(manager1);
        Assert.NotNull(qaTeam);

        _currentUserService.SetUser(manager1.Id, manager1.Email!, UserRole.Manager.ToString());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _teamService.GetTeamByIdAsync(qaTeam.Id));
    }

    [Fact]
    public async Task User_GetTeamById_ForOwnTeam_Succeeds()
    {
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        var engTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "Engineering Team");
        Assert.NotNull(dev1);
        Assert.NotNull(engTeam);

        _currentUserService.SetUser(dev1.Id, dev1.Email!, UserRole.User.ToString());

        var result = await _teamService.GetTeamByIdAsync(engTeam.Id);

        Assert.NotNull(result);
        Assert.Equal(engTeam.Id, result.Id);
    }

    [Fact]
    public async Task User_GetTeamById_ForOtherTeam_ThrowsForbidden_IDOR()
    {
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        var qaTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "QA & Testing Team");
        Assert.NotNull(dev1);
        Assert.NotNull(qaTeam);

        _currentUserService.SetUser(dev1.Id, dev1.Email!, UserRole.User.ToString());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _teamService.GetTeamByIdAsync(qaTeam.Id));
    }

    [Fact]
    public async Task Admin_UpdateTeam_Succeeds()
    {
        var admin = await _userManager.FindByEmailAsync("admin@taskmanagement.com");
        var engTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "Engineering Team");
        Assert.NotNull(admin);
        Assert.NotNull(engTeam);

        _currentUserService.SetUser(admin.Id, admin.Email!, UserRole.Admin.ToString());

        var result = await _teamService.UpdateTeamAsync(engTeam.Id, new UpdateTeamRequest
        {
            Name = "Platform Engineering Team"
        });

        Assert.NotNull(result);
        Assert.Equal("Platform Engineering Team", result.Name);
    }

    [Fact]
    public async Task Repository_NameExistsAsync_CaseInsensitive_And_ExcludeTeamId_Behavior()
    {
        var engTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "Engineering Team");
        var qaTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "QA & Testing Team");
        Assert.NotNull(engTeam);
        Assert.NotNull(qaTeam);

        // Case-insensitive detection ("Engineering Team" vs "engineering team")
        var existsLower = await _teamRepository.NameExistsAsync("engineering team");
        Assert.True(existsLower);

        // excludeTeamId allows the existing team itself
        var existsExcludedSelf = await _teamRepository.NameExistsAsync("engineering team", engTeam.Id);
        Assert.False(existsExcludedSelf);

        // excludeTeamId still detects collision with another team
        var existsConflictOther = await _teamRepository.NameExistsAsync("qa & testing team", engTeam.Id);
        Assert.True(existsConflictOther);
    }

    [Fact]
    public async Task Admin_CreateTeam_CaseInsensitiveDuplicateName_ThrowsConflict()
    {
        var admin = await _userManager.FindByEmailAsync("admin@taskmanagement.com");
        var manager2 = await _userManager.FindByEmailAsync("manager2@taskmanagement.com");
        Assert.NotNull(admin);
        Assert.NotNull(manager2);

        _currentUserService.SetUser(admin.Id, admin.Email!, UserRole.Admin.ToString());

        await Assert.ThrowsAsync<ConflictException>(() =>
            _teamService.CreateTeamAsync(new CreateTeamRequest
            {
                Name = "engineering team",
                ManagerId = manager2.Id
            }));
    }

    [Fact]
    public async Task Admin_UpdateTeam_SameName_Succeeds_DueToExcludeTeamId()
    {
        var admin = await _userManager.FindByEmailAsync("admin@taskmanagement.com");
        var engTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "Engineering Team");
        Assert.NotNull(admin);
        Assert.NotNull(engTeam);

        _currentUserService.SetUser(admin.Id, admin.Email!, UserRole.Admin.ToString());

        var result = await _teamService.UpdateTeamAsync(engTeam.Id, new UpdateTeamRequest
        {
            Name = "engineering team"
        });

        Assert.NotNull(result);
        Assert.Equal("engineering team", result.Name);
    }

    [Fact]
    public async Task Admin_UpdateTeam_ConflictWithOtherTeam_ThrowsConflict()
    {
        var admin = await _userManager.FindByEmailAsync("admin@taskmanagement.com");
        var engTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "Engineering Team");
        Assert.NotNull(admin);
        Assert.NotNull(engTeam);

        _currentUserService.SetUser(admin.Id, admin.Email!, UserRole.Admin.ToString());

        await Assert.ThrowsAsync<ConflictException>(() =>
            _teamService.UpdateTeamAsync(engTeam.Id, new UpdateTeamRequest
            {
                Name = "qa & testing team"
            }));
    }

    [Fact]
    public async Task Manager_UpdateTeam_ThrowsForbidden()
    {
        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        var engTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "Engineering Team");
        Assert.NotNull(manager1);
        Assert.NotNull(engTeam);

        _currentUserService.SetUser(manager1.Id, manager1.Email!, UserRole.Manager.ToString());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _teamService.UpdateTeamAsync(engTeam.Id, new UpdateTeamRequest { Name = "New Name" }));
    }

    [Fact]
    public async Task Manager_AddMember_ToOwnTeam_Succeeds()
    {
        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        var engTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "Engineering Team");
        Assert.NotNull(manager1);
        Assert.NotNull(engTeam);

        // Create a new unassigned user
        var unassignedUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "newbie@taskmanagement.com",
            Email = "newbie@taskmanagement.com",
            Name = "Newbie User",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _userManager.CreateAsync(unassignedUser, "Password123!");
        await _userManager.AddToRoleAsync(unassignedUser, UserRole.User.ToString());

        _currentUserService.SetUser(manager1.Id, manager1.Email!, UserRole.Manager.ToString());

        var result = await _teamService.AddMemberAsync(engTeam.Id, unassignedUser.Id);

        Assert.NotNull(result);
        Assert.Contains(result.Members, m => m.Id == unassignedUser.Id);
    }

    [Fact]
    public async Task Manager_AddMember_ToOtherTeam_ThrowsForbidden_IDOR()
    {
        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        var qaTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "QA & Testing Team");
        Assert.NotNull(manager1);
        Assert.NotNull(qaTeam);

        _currentUserService.SetUser(manager1.Id, manager1.Email!, UserRole.Manager.ToString());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _teamService.AddMemberAsync(qaTeam.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task User_AddMember_ThrowsForbidden()
    {
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        var engTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "Engineering Team");
        Assert.NotNull(dev1);
        Assert.NotNull(engTeam);

        _currentUserService.SetUser(dev1.Id, dev1.Email!, UserRole.User.ToString());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _teamService.AddMemberAsync(engTeam.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task AddMember_WhenAlreadyInTeam_ThrowsConflict()
    {
        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        var engTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "Engineering Team");
        Assert.NotNull(manager1);
        Assert.NotNull(dev1);
        Assert.NotNull(engTeam);

        _currentUserService.SetUser(manager1.Id, manager1.Email!, UserRole.Manager.ToString());

        await Assert.ThrowsAsync<ConflictException>(() =>
            _teamService.AddMemberAsync(engTeam.Id, dev1.Id));
    }

    [Fact]
    public async Task AddMember_WhenInAnotherTeam_ThrowsConflict()
    {
        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        var qa1 = await _userManager.FindByEmailAsync("qa1@taskmanagement.com");
        var engTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "Engineering Team");
        Assert.NotNull(manager1);
        Assert.NotNull(qa1);
        Assert.NotNull(engTeam);

        _currentUserService.SetUser(manager1.Id, manager1.Email!, UserRole.Manager.ToString());

        await Assert.ThrowsAsync<ConflictException>(() =>
            _teamService.AddMemberAsync(engTeam.Id, qa1.Id));
    }

    [Fact]
    public async Task Manager_RemoveMember_FromOwnTeam_Succeeds()
    {
        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        var dev2 = await _userManager.FindByEmailAsync("dev2@taskmanagement.com");
        var engTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "Engineering Team");
        Assert.NotNull(manager1);
        Assert.NotNull(dev2);
        Assert.NotNull(engTeam);

        _currentUserService.SetUser(manager1.Id, manager1.Email!, UserRole.Manager.ToString());

        var result = await _teamService.RemoveMemberAsync(engTeam.Id, dev2.Id);

        Assert.NotNull(result);
        Assert.DoesNotContain(result.Members, m => m.Id == dev2.Id);

        var refreshedDev2 = await _userManager.FindByIdAsync(dev2.Id.ToString());
        Assert.NotNull(refreshedDev2);
        Assert.Null(refreshedDev2.TeamId);
    }

    [Fact]
    public async Task Manager_RemoveMember_FromOtherTeam_ThrowsForbidden_IDOR()
    {
        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        var qa1 = await _userManager.FindByEmailAsync("qa1@taskmanagement.com");
        var qaTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "QA & Testing Team");
        Assert.NotNull(manager1);
        Assert.NotNull(qa1);
        Assert.NotNull(qaTeam);

        _currentUserService.SetUser(manager1.Id, manager1.Email!, UserRole.Manager.ToString());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _teamService.RemoveMemberAsync(qaTeam.Id, qa1.Id));
    }

    [Fact]
    public async Task User_RemoveMember_ThrowsForbidden()
    {
        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        var dev2 = await _userManager.FindByEmailAsync("dev2@taskmanagement.com");
        var engTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "Engineering Team");
        Assert.NotNull(dev1);
        Assert.NotNull(dev2);
        Assert.NotNull(engTeam);

        _currentUserService.SetUser(dev1.Id, dev1.Email!, UserRole.User.ToString());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _teamService.RemoveMemberAsync(engTeam.Id, dev2.Id));
    }

    [Fact]
    public async Task RemoveMember_UserNotInTeam_ThrowsValidation()
    {
        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        var qa1 = await _userManager.FindByEmailAsync("qa1@taskmanagement.com");
        var engTeam = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name == "Engineering Team");
        Assert.NotNull(manager1);
        Assert.NotNull(qa1);
        Assert.NotNull(engTeam);

        _currentUserService.SetUser(manager1.Id, manager1.Email!, UserRole.Manager.ToString());

        await Assert.ThrowsAsync<ValidationException>(() =>
            _teamService.RemoveMemberAsync(engTeam.Id, qa1.Id));
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
