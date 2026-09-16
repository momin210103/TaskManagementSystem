using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs.Users;
using TaskManagement.Application.Interfaces;
using TaskManagement.Infrastructure.Identity;

namespace TaskManagement.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public UserRepository(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(roleManager);

        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<UserResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        return MapToUserResponse(user, roles.FirstOrDefault() ?? string.Empty);
    }

    public async Task<UserResponse?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);
        var normalizedEmail = email.ToUpperInvariant();
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        return MapToUserResponse(user, roles.FirstOrDefault() ?? string.Empty);
    }

    public async Task<PagedResult<UserResponse>> GetPagedAsync(
        int page,
        int pageSize,
        Guid? teamId = null,
        string? role = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Users.AsNoTracking().AsQueryable();

        if (teamId.HasValue)
        {
            query = query.Where(u => u.TeamId == teamId.Value);
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var normalizedRoleName = role.ToUpperInvariant();
            var roleEntity = await _roleManager.Roles.FirstOrDefaultAsync(r => r.NormalizedName == normalizedRoleName, cancellationToken).ConfigureAwait(false);
            if (roleEntity is not null)
            {
                var userIdsInRole = _context.UserRoles.Where(ur => ur.RoleId == roleEntity.Id).Select(ur => ur.UserId);
                query = query.Where(u => userIdsInRole.Contains(u.Id));
            }
            else
            {
                query = query.Where(u => false);
            }
        }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var effectivePage = Math.Max(1, page);
        var effectivePageSize = Math.Clamp(pageSize, 1, 100);

        var users = await query
            .OrderBy(u => u.Name)
            .Skip((effectivePage - 1) * effectivePageSize)
            .Take(effectivePageSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var userIds = users.Select(u => u.Id).ToList();
        var userRoles = await (from ur in _context.UserRoles
                               join r in _context.Roles on ur.RoleId equals r.Id
                               where userIds.Contains(ur.UserId)
                               select new { ur.UserId, RoleName = r.Name })
                               .ToListAsync(cancellationToken).ConfigureAwait(false);

        var items = users.Select(u =>
        {
            var userRole = userRoles.FirstOrDefault(ur => ur.UserId == u.Id)?.RoleName ?? string.Empty;
            return MapToUserResponse(u, userRole);
        }).ToList();

        return new PagedResult<UserResponse>
        {
            Items = items,
            Page = effectivePage,
            PageSize = effectivePageSize,
            TotalCount = totalCount
        };
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users.AnyAsync(u => u.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> TeamExistsAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        return await _context.Teams.AnyAsync(t => t.Id == teamId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Guid?> GetUserTeamIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.TeamId)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> UpdateRoleAsync(Guid userId, string newRole, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(newRole);
        var user = await _userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null)
        {
            return false;
        }

        var currentRoles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        if (currentRoles.Count > 0)
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles).ConfigureAwait(false);
        }

        await _userManager.AddToRoleAsync(user, newRole).ConfigureAwait(false);
        user.UpdatedAt = DateTime.UtcNow;
        var result = await _userManager.UpdateAsync(user).ConfigureAwait(false);
        return result.Succeeded;
    }

    public async Task<bool> UpdateTeamAsync(Guid userId, Guid? teamId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null)
        {
            return false;
        }

        user.TeamId = teamId;
        user.UpdatedAt = DateTime.UtcNow;
        var result = await _userManager.UpdateAsync(user).ConfigureAwait(false);
        return result.Succeeded;
    }

    private static UserResponse MapToUserResponse(ApplicationUser user, string role)
    {
        return new UserResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email ?? string.Empty,
            Role = role,
            TeamId = user.TeamId,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
