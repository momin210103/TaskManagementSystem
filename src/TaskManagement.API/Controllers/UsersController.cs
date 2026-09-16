using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs.Users;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        ArgumentNullException.ThrowIfNull(userService);
        _userService = userService;
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userService.GetCurrentUserAsync(cancellationToken).ConfigureAwait(false);
            return Ok(user);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "User not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(PagedResult<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] UserQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _userService.GetUsersAsync(parameters, cancellationToken).ConfigureAwait(false);
            return Ok(result);
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Forbidden",
                Detail = ex.Message,
                Status = StatusCodes.Status403Forbidden
            });
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(id, cancellationToken).ConfigureAwait(false);
            return Ok(user);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "User not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Forbidden",
                Detail = ex.Message,
                Status = StatusCodes.Status403Forbidden
            });
        }
    }

    [HttpPatch("{id:guid}/role")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserRole(
        Guid id,
        [FromBody] ChangeUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userService.UpdateUserRoleAsync(id, request, cancellationToken).ConfigureAwait(false);
            return Ok(user);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "User not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Forbidden",
                Detail = ex.Message,
                Status = StatusCodes.Status403Forbidden
            });
        }
    }

    [HttpPatch("{id:guid}/team")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserTeam(
        Guid id,
        [FromBody] AssignUserTeamRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userService.UpdateUserTeamAsync(id, request, cancellationToken).ConfigureAwait(false);
            return Ok(user);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Resource not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Forbidden",
                Detail = ex.Message,
                Status = StatusCodes.Status403Forbidden
            });
        }
    }
}

