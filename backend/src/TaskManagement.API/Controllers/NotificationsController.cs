using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs.Notifications;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        ArgumentNullException.ThrowIfNull(notificationService);
        _notificationService = notificationService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<NotificationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] NotificationQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _notificationService.GetNotificationsAsync(parameters, cancellationToken).ConfigureAwait(false);
            return Ok(result);
        }
        catch (AuthException ex)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = ex.Message,
                Status = StatusCodes.Status401Unauthorized
            });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpPatch("{id:guid}/read")]
    [ProducesResponseType(typeof(NotificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _notificationService.MarkAsReadAsync(id, cancellationToken).ConfigureAwait(false);
            return Ok(result);
        }
        catch (AuthException ex)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = ex.Message,
                Status = StatusCodes.Status401Unauthorized
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
        catch (NotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpPatch("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        try
        {
            await _notificationService.MarkAllAsReadAsync(cancellationToken).ConfigureAwait(false);
            return NoContent();
        }
        catch (AuthException ex)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = ex.Message,
                Status = StatusCodes.Status401Unauthorized
            });
        }
    }
}
