using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Identity;
using TaskManagement.Infrastructure.Persistence;
using Xunit;

namespace TaskManagement.UnitTests;

public sealed class DatabaseSeederTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public DatabaseSeederTests()
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
    }

    [Fact]
    public async Task SeedAsync_PopulatesAllExpectedData_Successfully()
    {
        await DatabaseSeeder.SeedAsync(_serviceProvider);

        // Verify Roles
        Assert.True(await _roleManager.RoleExistsAsync(UserRole.Admin.ToString()));
        Assert.True(await _roleManager.RoleExistsAsync(UserRole.Manager.ToString()));
        Assert.True(await _roleManager.RoleExistsAsync(UserRole.User.ToString()));

        // Verify Users
        var admin = await _userManager.FindByEmailAsync("admin@taskmanagement.com");
        Assert.NotNull(admin);
        Assert.True(await _userManager.IsInRoleAsync(admin, UserRole.Admin.ToString()));

        var manager1 = await _userManager.FindByEmailAsync("manager1@taskmanagement.com");
        Assert.NotNull(manager1);
        Assert.True(await _userManager.IsInRoleAsync(manager1, UserRole.Manager.ToString()));

        var dev1 = await _userManager.FindByEmailAsync("dev1@taskmanagement.com");
        Assert.NotNull(dev1);
        Assert.True(await _userManager.IsInRoleAsync(dev1, UserRole.User.ToString()));

        // Verify Teams
        var teams = await _dbContext.Teams.ToListAsync();
        Assert.Equal(2, teams.Count);

        var engTeam = teams.FirstOrDefault(t => string.Equals(t.Name, "Engineering Team", StringComparison.Ordinal));
        Assert.NotNull(engTeam);
        Assert.Equal(manager1.Id, engTeam.ManagerId);
        Assert.Equal(engTeam.Id, manager1.TeamId);
        Assert.Equal(engTeam.Id, dev1.TeamId);

        // Verify Tasks
        var tasks = await _dbContext.Tasks.ToListAsync();
        Assert.Equal(3, tasks.Count);

        var task1 = tasks.FirstOrDefault(t => string.Equals(t.Title, "Implement Authentication Module", StringComparison.Ordinal));
        Assert.NotNull(task1);
        Assert.Equal(engTeam.Id, task1.TeamId);
        Assert.Equal(dev1.Id, task1.AssignedToId);
        Assert.Equal(manager1.Id, task1.AssignedById);
        Assert.Equal(TaskManagement.Domain.Enums.TaskStatus.InProgress, task1.Status);
        Assert.Equal(TaskPriority.High, task1.Priority);

        // Verify Comments
        var comments = await _dbContext.Comments.ToListAsync();
        Assert.Equal(3, comments.Count);

        // Verify Notifications
        var notifications = await _dbContext.Notifications.ToListAsync();
        Assert.Equal(3, notifications.Count);
    }

    [Fact]
    public async Task SeedAsync_IsIdempotent_CanRunMultipleTimes()
    {
        await DatabaseSeeder.SeedAsync(_serviceProvider);
        await DatabaseSeeder.SeedAsync(_serviceProvider);

        var roles = await _roleManager.Roles.ToListAsync();
        Assert.Equal(3, roles.Count);

        var users = await _userManager.Users.ToListAsync();
        Assert.Equal(6, users.Count);

        var teams = await _dbContext.Teams.ToListAsync();
        Assert.Equal(2, teams.Count);

        var tasks = await _dbContext.Tasks.ToListAsync();
        Assert.Equal(3, tasks.Count);

        var comments = await _dbContext.Comments.ToListAsync();
        Assert.Equal(3, comments.Count);

        var notifications = await _dbContext.Notifications.ToListAsync();
        Assert.Equal(3, notifications.Count);
    }

    [Fact]
    public async Task SeedAsync_ThrowsArgumentNullException_WhenServiceProviderIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => DatabaseSeeder.SeedAsync(null!));
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _userManager.Dispose();
        _roleManager.Dispose();
        _serviceProvider.Dispose();
    }
}

