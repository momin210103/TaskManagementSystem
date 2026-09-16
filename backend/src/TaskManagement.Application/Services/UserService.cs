using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs.Users;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public UserService(IUserRepository userRepository, ICurrentUserService currentUserService)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(currentUserService);

        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task<UserResponse> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserIdOrThrow();
        var user = await _userRepository.GetByIdAsync(currentUserId, cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            throw new NotFoundException($"User with ID '{currentUserId}' was not found.");
        }

        return user;
    }

    public async Task<PagedResult<UserResponse>> GetUsersAsync(
        UserQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        var currentUserId = GetCurrentUserIdOrThrow();

        if (_currentUserService.IsInRole(UserRole.Admin.ToString()))
        {
            return await _userRepository.GetPagedAsync(
                parameters.Page,
                parameters.PageSize,
                parameters.TeamId,
                parameters.Role,
                cancellationToken).ConfigureAwait(false);
        }

        if (_currentUserService.IsInRole(UserRole.Manager.ToString()))
        {
            var managerTeamId = await _userRepository.GetUserTeamIdAsync(currentUserId, cancellationToken).ConfigureAwait(false);
            if (!managerTeamId.HasValue)
            {
                return new PagedResult<UserResponse>
                {
                    Items = [],
                    Page = parameters.Page,
                    PageSize = parameters.PageSize,
                    TotalCount = 0
                };
            }

            if (parameters.TeamId.HasValue && parameters.TeamId.Value != managerTeamId.Value)
            {
                throw new ForbiddenException("Managers can only view users in their own team.");
            }

            return await _userRepository.GetPagedAsync(
                parameters.Page,
                parameters.PageSize,
                managerTeamId.Value,
                parameters.Role,
                cancellationToken).ConfigureAwait(false);
        }

        throw new ForbiddenException("Users are not permitted to list organization users.");
    }

    public async Task<UserResponse> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserIdOrThrow();
        var user = await _userRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            throw new NotFoundException($"User with ID '{id}' was not found.");
        }

        await AuthorizeViewUserAsync(user, currentUserId, cancellationToken).ConfigureAwait(false);

        return user;
    }

    public async Task<UserResponse> UpdateUserRoleAsync(
        Guid id,
        ChangeUserRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_currentUserService.IsInRole(UserRole.Admin.ToString()))
        {
            throw new ForbiddenException("Only Admins can change user roles.");
        }

        var normalizedRole = NormalizeAndValidateRole(request.Role);

        if (!await _userRepository.ExistsAsync(id, cancellationToken).ConfigureAwait(false))
        {
            throw new NotFoundException($"User with ID '{id}' was not found.");
        }

        await _userRepository.UpdateRoleAsync(id, normalizedRole, cancellationToken).ConfigureAwait(false);

        var updatedUser = await _userRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return updatedUser ?? throw new NotFoundException($"User with ID '{id}' was not found.");
    }

    public async Task<UserResponse> UpdateUserTeamAsync(
        Guid id,
        AssignUserTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_currentUserService.IsInRole(UserRole.Admin.ToString()))
        {
            throw new ForbiddenException("Only Admins can assign users to teams.");
        }

        if (!await _userRepository.ExistsAsync(id, cancellationToken).ConfigureAwait(false))
        {
            throw new NotFoundException($"User with ID '{id}' was not found.");
        }

        if (request.TeamId.HasValue && !await _userRepository.TeamExistsAsync(request.TeamId.Value, cancellationToken).ConfigureAwait(false))
        {
            throw new NotFoundException($"Team with ID '{request.TeamId.Value}' was not found.");
        }

        await _userRepository.UpdateTeamAsync(id, request.TeamId, cancellationToken).ConfigureAwait(false);

        var updatedUser = await _userRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return updatedUser ?? throw new NotFoundException($"User with ID '{id}' was not found.");
    }

    private Guid GetCurrentUserIdOrThrow()
    {
        return _currentUserService.UserId ?? throw new AuthException("User is not authenticated.");
    }

    private async Task AuthorizeViewUserAsync(UserResponse user, Guid currentUserId, CancellationToken cancellationToken)
    {
        if (_currentUserService.IsInRole(UserRole.Admin.ToString()) || user.Id == currentUserId)
        {
            return;
        }

        if (_currentUserService.IsInRole(UserRole.Manager.ToString()))
        {
            var managerTeamId = await _userRepository.GetUserTeamIdAsync(currentUserId, cancellationToken).ConfigureAwait(false);
            if (managerTeamId.HasValue && user.TeamId.HasValue && managerTeamId.Value == user.TeamId.Value)
            {
                return;
            }

            throw new ForbiddenException("Managers can only view users belonging to their own team.");
        }

        throw new ForbiddenException("Users can only view their own profile.");
    }

    private static string NormalizeAndValidateRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ValidationException("Role is required.");
        }

        if (string.Equals(role, UserRole.Admin.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return UserRole.Admin.ToString();
        }

        if (string.Equals(role, UserRole.Manager.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return UserRole.Manager.ToString();
        }

        if (string.Equals(role, UserRole.User.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return UserRole.User.ToString();
        }

        throw new ValidationException($"Invalid role '{role}'. Allowed roles are: Admin, Manager, User.");
    }
}

