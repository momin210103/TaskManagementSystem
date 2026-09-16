using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.DTOs.Teams;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Infrastructure.Identity;

namespace TaskManagement.Infrastructure.Persistence.Repositories;

public class TeamRepository : ITeamRepository
{
    private readonly ApplicationDbContext _context;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public TeamRepository(
        ApplicationDbContext context,
        RoleManager<IdentityRole<Guid>> roleManager)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(roleManager);

        _context = context;
        _roleManager = roleManager;
    }

    public async Task<Team?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Teams
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TeamResponse?> GetResponseByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var team = await _context.Teams
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (team is null)
        {
            return null;
        }

        var manager = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == team.ManagerId, cancellationToken)
            .ConfigureAwait(false);

        var memberCount = await _context.Users
            .AsNoTracking()
            .CountAsync(u => u.TeamId == id, cancellationToken)
            .ConfigureAwait(false);

        return new TeamResponse
        {
            Id = team.Id,
            Name = team.Name,
            ManagerId = team.ManagerId,
            ManagerName = manager?.Name ?? string.Empty,
            ManagerEmail = manager?.Email ?? string.Empty,
            MemberCount = memberCount,
            CreatedAt = team.CreatedAt,
            UpdatedAt = team.UpdatedAt
        };
    }

    public async Task<TeamDetailsResponse?> GetDetailsByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var team = await _context.Teams
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (team is null)
        {
            return null;
        }

        var manager = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == team.ManagerId, cancellationToken)
            .ConfigureAwait(false);

        var members = await _context.Users
            .AsNoTracking()
            .Where(u => u.TeamId == id)
            .OrderBy(u => u.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var memberIds = members.Select(m => m.Id).ToList();
        var userRoles = await (from ur in _context.UserRoles
                               join r in _context.Roles on ur.RoleId equals r.Id
                               where memberIds.Contains(ur.UserId)
                               select new { ur.UserId, RoleName = r.Name })
                               .ToListAsync(cancellationToken)
                               .ConfigureAwait(false);

        var memberResponses = members.Select(m => new TeamMemberResponse
        {
            Id = m.Id,
            Name = m.Name,
            Email = m.Email ?? string.Empty,
            Role = userRoles.FirstOrDefault(ur => ur.UserId == m.Id)?.RoleName ?? string.Empty
        }).ToList();

        return new TeamDetailsResponse
        {
            Id = team.Id,
            Name = team.Name,
            ManagerId = team.ManagerId,
            ManagerName = manager?.Name ?? string.Empty,
            ManagerEmail = manager?.Email ?? string.Empty,
            Members = memberResponses,
            CreatedAt = team.CreatedAt,
            UpdatedAt = team.UpdatedAt
        };
    }

    public async Task<IReadOnlyList<TeamResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var teams = await _context.Teams
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return await MapToTeamResponsesAsync(teams, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TeamResponse>> GetByManagerIdAsync(Guid managerId, CancellationToken cancellationToken = default)
    {
        var teams = await _context.Teams
            .AsNoTracking()
            .Where(t => t.ManagerId == managerId)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return await MapToTeamResponsesAsync(teams, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TeamResponse?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            .ConfigureAwait(false);

        if (user?.TeamId is null)
        {
            return null;
        }

        return await GetResponseByIdAsync(user.TeamId.Value, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Teams
            .AnyAsync(t => t.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> NameExistsAsync(string name, Guid? excludeTeamId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(name);

        var trimmedName = name.Trim();
        var query = _context.Teams.AsNoTracking();

        if (excludeTeamId.HasValue)
        {
            query = query.Where(t => t.Id != excludeTeamId.Value);
        }

        if (string.Equals(_context.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal))
        {
            return await query.AnyAsync(t => string.Equals(t.Name, trimmedName, StringComparison.OrdinalIgnoreCase), cancellationToken).ConfigureAwait(false);
        }

        return await query.AnyAsync(t => EF.Functions.ILike(t.Name, trimmedName), cancellationToken).ConfigureAwait(false);
    }

    public async Task<Team> CreateAsync(Team team, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(team);

        _context.Teams.Add(team);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return team;
    }

    public async Task<bool> UpdateAsync(Team team, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(team);

        _context.Teams.Update(team);
        var affected = await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return affected > 0;
    }

    public async Task<bool> AddMemberAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            return false;
        }

        user.TeamId = teamId;
        user.UpdatedAt = DateTime.UtcNow;
        var affected = await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return affected > 0;
    }

    public async Task<bool> RemoveMemberAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null || user.TeamId != teamId)
        {
            return false;
        }

        user.TeamId = null;
        user.UpdatedAt = DateTime.UtcNow;
        var affected = await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return affected > 0;
    }

    public async Task<bool> IsUserInRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(role);

        var normalizedRole = role.ToUpperInvariant();
        var roleEntity = await _roleManager.Roles
            .FirstOrDefaultAsync(r => r.NormalizedName == normalizedRole, cancellationToken)
            .ConfigureAwait(false);

        if (roleEntity is null)
        {
            return false;
        }

        return await _context.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleEntity.Id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AnyAsync(u => u.Id == userId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Guid?> GetUserTeamIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.TeamId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<TeamResponse>> MapToTeamResponsesAsync(
        List<Team> teams,
        CancellationToken cancellationToken)
    {
        if (teams.Count == 0)
        {
            return [];
        }

        var managerIds = teams.Select(t => t.ManagerId).Distinct().ToList();
        var managers = await _context.Users
            .AsNoTracking()
            .Where(u => managerIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken)
            .ConfigureAwait(false);

        var teamIds = teams.Select(t => t.Id).ToList();
        var memberCounts = await _context.Users
            .AsNoTracking()
            .Where(u => u.TeamId.HasValue && teamIds.Contains(u.TeamId.Value))
            .GroupBy(u => u.TeamId!.Value)
            .Select(g => new { TeamId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TeamId, x => x.Count, cancellationToken)
            .ConfigureAwait(false);

        return teams.Select(t =>
        {
            managers.TryGetValue(t.ManagerId, out var mgr);
            memberCounts.TryGetValue(t.Id, out var count);
            return new TeamResponse
            {
                Id = t.Id,
                Name = t.Name,
                ManagerId = t.ManagerId,
                ManagerName = mgr?.Name ?? string.Empty,
                ManagerEmail = mgr?.Email ?? string.Empty,
                MemberCount = count,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            };
        }).ToList();
    }
}
