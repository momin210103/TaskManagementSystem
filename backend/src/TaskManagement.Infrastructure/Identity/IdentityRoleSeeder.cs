using Microsoft.AspNetCore.Identity;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Infrastructure.Identity;

public static class IdentityRoleSeeder
{
    private static readonly string[] Roles =
    [
        UserRole.Admin.ToString(),
        UserRole.Manager.ToString(),
        UserRole.User.ToString()
    ];

    public static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        ArgumentNullException.ThrowIfNull(roleManager);

        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role).ConfigureAwait(false))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role)).ConfigureAwait(false);
            }
        }
    }
}

