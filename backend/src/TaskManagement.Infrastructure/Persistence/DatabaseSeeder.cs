using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Identity;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    private const string DefaultPassword = "Password123!";

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await IdentityRoleSeeder.SeedRolesAsync(roleManager).ConfigureAwait(false);
        var users = await SeedUsersAsync(userManager).ConfigureAwait(false);
        var teams = await SeedTeamsAsync(context, users).ConfigureAwait(false);
        var tasks = await SeedTasksAsync(context, teams, users).ConfigureAwait(false);
        await SeedCommentsAsync(context, tasks, users).ConfigureAwait(false);
        await SeedNotificationsAsync(context, tasks, users).ConfigureAwait(false);
    }

    private static async Task<Dictionary<string, ApplicationUser>> SeedUsersAsync(
        UserManager<ApplicationUser> userManager)
    {
        var seedUsers = new (string Email, string Name, string Role)[]
        {
            ("admin@taskmanagement.com", "System Admin", UserRole.Admin.ToString()),
            ("manager1@taskmanagement.com", "Sarah Manager", UserRole.Manager.ToString()),
            ("manager2@taskmanagement.com", "Alex Manager", UserRole.Manager.ToString()),
            ("dev1@taskmanagement.com", "John Developer", UserRole.User.ToString()),
            ("dev2@taskmanagement.com", "Jane Developer", UserRole.User.ToString()),
            ("qa1@taskmanagement.com", "Bob Tester", UserRole.User.ToString())
        };

        var userMap = new Dictionary<string, ApplicationUser>(StringComparer.OrdinalIgnoreCase);
        var now = DateTime.UtcNow;

        foreach (var (email, name, role) in seedUsers)
        {
            var user = await userManager.FindByEmailAsync(email).ConfigureAwait(false);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = email,
                    Email = email,
                    Name = name,
                    EmailConfirmed = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                var result = await userManager.CreateAsync(user, DefaultPassword).ConfigureAwait(false);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role).ConfigureAwait(false);
                }
            }

            userMap[email] = user;
        }

        return userMap;
    }

    private static async Task<Dictionary<string, Team>> SeedTeamsAsync(
        ApplicationDbContext context,
        Dictionary<string, ApplicationUser> users)
    {
        var teamMap = new Dictionary<string, Team>(StringComparer.OrdinalIgnoreCase);
        var now = DateTime.UtcNow;

        var engTeam = await context.Teams.FirstOrDefaultAsync(t => t.Name == "Engineering Team").ConfigureAwait(false);
        if (engTeam is null)
        {
            engTeam = new Team
            {
                Id = Guid.NewGuid(),
                Name = "Engineering Team",
                ManagerId = users["manager1@taskmanagement.com"].Id,
                CreatedAt = now,
                UpdatedAt = now
            };
            await context.Teams.AddAsync(engTeam).ConfigureAwait(false);
        }
        teamMap["Engineering Team"] = engTeam;

        var qaTeam = await context.Teams.FirstOrDefaultAsync(t => t.Name == "QA & Testing Team").ConfigureAwait(false);
        if (qaTeam is null)
        {
            qaTeam = new Team
            {
                Id = Guid.NewGuid(),
                Name = "QA & Testing Team",
                ManagerId = users["manager2@taskmanagement.com"].Id,
                CreatedAt = now,
                UpdatedAt = now
            };
            await context.Teams.AddAsync(qaTeam).ConfigureAwait(false);
        }
        teamMap["QA & Testing Team"] = qaTeam;

        await context.SaveChangesAsync().ConfigureAwait(false);

        AssignUserTeam(context, users["manager1@taskmanagement.com"], engTeam.Id);
        AssignUserTeam(context, users["dev1@taskmanagement.com"], engTeam.Id);
        AssignUserTeam(context, users["dev2@taskmanagement.com"], engTeam.Id);
        AssignUserTeam(context, users["manager2@taskmanagement.com"], qaTeam.Id);
        AssignUserTeam(context, users["qa1@taskmanagement.com"], qaTeam.Id);

        await context.SaveChangesAsync().ConfigureAwait(false);

        return teamMap;
    }

    private static void AssignUserTeam(ApplicationDbContext context, ApplicationUser user, Guid teamId)
    {
        if (user.TeamId != teamId)
        {
            user.TeamId = teamId;
            context.Users.Update(user);
        }
    }

    private static async Task<Dictionary<string, TaskItem>> SeedTasksAsync(
        ApplicationDbContext context,
        Dictionary<string, Team> teams,
        Dictionary<string, ApplicationUser> users)
    {
        var taskMap = new Dictionary<string, TaskItem>(StringComparer.OrdinalIgnoreCase);
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        var task1 = await CreateOrGetTaskAsync(
            context,
            "Implement Authentication Module",
            "Set up ASP.NET Core Identity with JWT and refresh token rotation.",
            TaskStatus.InProgress,
            TaskPriority.High,
            today.AddDays(7),
            teams["Engineering Team"].Id,
            users["dev1@taskmanagement.com"].Id,
            users["manager1@taskmanagement.com"].Id,
            now).ConfigureAwait(false);
        taskMap["Implement Authentication Module"] = task1;

        var task2 = await CreateOrGetTaskAsync(
            context,
            "Design Database Schema",
            "Create EF Core fluent configurations and initial migrations for PostgreSQL.",
            TaskStatus.Done,
            TaskPriority.Medium,
            today.AddDays(-2),
            teams["Engineering Team"].Id,
            users["dev2@taskmanagement.com"].Id,
            users["manager1@taskmanagement.com"].Id,
            now).ConfigureAwait(false);
        taskMap["Design Database Schema"] = task2;

        var task3 = await CreateOrGetTaskAsync(
            context,
            "Setup Integration Test Suite",
            "Write xUnit integration tests covering end-to-end task workflows.",
            TaskStatus.ToDo,
            TaskPriority.High,
            today.AddDays(14),
            teams["QA & Testing Team"].Id,
            users["qa1@taskmanagement.com"].Id,
            users["manager2@taskmanagement.com"].Id,
            now).ConfigureAwait(false);
        taskMap["Setup Integration Test Suite"] = task3;

        await context.SaveChangesAsync().ConfigureAwait(false);
        return taskMap;
    }

    private static async Task<TaskItem> CreateOrGetTaskAsync(
        ApplicationDbContext context,
        string title,
        string description,
        TaskStatus status,
        TaskPriority priority,
        DateOnly deadline,
        Guid teamId,
        Guid assignedToId,
        Guid assignedById,
        DateTime now)
    {
        var task = await context.Tasks.FirstOrDefaultAsync(t => t.Title == title).ConfigureAwait(false);
        if (task is null)
        {
            task = new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = description,
                Status = status,
                Priority = priority,
                Deadline = deadline,
                TeamId = teamId,
                AssignedToId = assignedToId,
                AssignedById = assignedById,
                CreatedAt = now,
                UpdatedAt = now
            };
            await context.Tasks.AddAsync(task).ConfigureAwait(false);
        }

        return task;
    }

    private static async Task SeedCommentsAsync(
        ApplicationDbContext context,
        Dictionary<string, TaskItem> tasks,
        Dictionary<string, ApplicationUser> users)
    {
        if (!await context.Comments.AnyAsync().ConfigureAwait(false))
        {
            var now = DateTime.UtcNow;
            var comments = new List<Comment>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    TaskId = tasks["Implement Authentication Module"].Id,
                    UserId = users["manager1@taskmanagement.com"].Id,
                    Content = "Please ensure refresh token hashing is applied.",
                    CreatedAt = now.AddHours(-3),
                    UpdatedAt = now.AddHours(-3)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    TaskId = tasks["Implement Authentication Module"].Id,
                    UserId = users["dev1@taskmanagement.com"].Id,
                    Content = "Working on it, using SHA-256 token hashing.",
                    CreatedAt = now.AddHours(-1),
                    UpdatedAt = now.AddHours(-1)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    TaskId = tasks["Design Database Schema"].Id,
                    UserId = users["dev2@taskmanagement.com"].Id,
                    Content = "All entity configurations are in place with delete behaviors configured.",
                    CreatedAt = now.AddDays(-1),
                    UpdatedAt = now.AddDays(-1)
                }
            };

            await context.Comments.AddRangeAsync(comments).ConfigureAwait(false);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    private static async Task SeedNotificationsAsync(
        ApplicationDbContext context,
        Dictionary<string, TaskItem> tasks,
        Dictionary<string, ApplicationUser> users)
    {
        if (!await context.Notifications.AnyAsync().ConfigureAwait(false))
        {
            var now = DateTime.UtcNow;
            var notifications = new List<Notification>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = users["dev1@taskmanagement.com"].Id,
                    TaskId = tasks["Implement Authentication Module"].Id,
                    Type = NotificationType.Assignment,
                    Message = "You have been assigned to task: Implement Authentication Module",
                    IsRead = false,
                    CreatedAt = now.AddDays(-2),
                    UpdatedAt = now.AddDays(-2)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = users["manager1@taskmanagement.com"].Id,
                    TaskId = tasks["Implement Authentication Module"].Id,
                    Type = NotificationType.StatusUpdate,
                    Message = "Task 'Implement Authentication Module' status updated to In Progress",
                    IsRead = false,
                    CreatedAt = now.AddHours(-1),
                    UpdatedAt = now.AddHours(-1)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = users["dev2@taskmanagement.com"].Id,
                    TaskId = tasks["Design Database Schema"].Id,
                    Type = NotificationType.Assignment,
                    Message = "You have been assigned to task: Design Database Schema",
                    IsRead = true,
                    CreatedAt = now.AddDays(-3),
                    UpdatedAt = now.AddDays(-3)
                }
            };

            await context.Notifications.AddRangeAsync(notifications).ConfigureAwait(false);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}

