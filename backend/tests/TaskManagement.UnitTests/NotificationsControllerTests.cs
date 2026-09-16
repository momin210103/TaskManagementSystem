using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.API.Controllers;
using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs.Notifications;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Enums;
using Xunit;

namespace TaskManagement.UnitTests;

public sealed class NotificationsControllerTests
{
    private readonly FakeNotificationService _notificationService = new();
    private readonly NotificationsController _controller;

    public NotificationsControllerTests()
    {
        _controller = new NotificationsController(_notificationService);
    }

    [Fact]
    public async Task GetNotifications_Returns200OK_WithPagedResult()
    {
        _notificationService.GetNotificationsResult = new PagedResult<NotificationResponse>
        {
            Items = [new NotificationResponse { Id = Guid.NewGuid(), Message = "Hello" }],
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };

        var result = await _controller.GetNotifications(new NotificationQueryParameters(), CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var paged = Assert.IsType<PagedResult<NotificationResponse>>(okResult.Value);
        Assert.Single(paged.Items);
    }

    [Fact]
    public async Task GetNotifications_WhenValidationException_Returns400BadRequest()
    {
        _notificationService.GetNotificationsException = new ValidationException("Invalid page");

        var result = await _controller.GetNotifications(new NotificationQueryParameters { Page = -1 }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal("Invalid page", problem.Detail);
    }

    [Fact]
    public async Task GetNotifications_WhenAuthException_Returns401Unauthorized()
    {
        _notificationService.GetNotificationsException = new AuthException("User is not authenticated.");

        var result = await _controller.GetNotifications(new NotificationQueryParameters(), CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    [Fact]
    public async Task MarkAsRead_Returns200OK_WithUpdatedNotification()
    {
        var notifId = Guid.NewGuid();
        _notificationService.MarkAsReadResult = new NotificationResponse
        {
            Id = notifId,
            IsRead = true,
            Message = "Task Assigned"
        };

        var result = await _controller.MarkAsRead(notifId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var response = Assert.IsType<NotificationResponse>(okResult.Value);
        Assert.True(response.IsRead);
    }

    [Fact]
    public async Task MarkAsRead_WhenNotFound_Returns404NotFound()
    {
        _notificationService.MarkAsReadException = new NotFoundException("Not found");

        var result = await _controller.MarkAsRead(Guid.NewGuid(), CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task MarkAsRead_WhenForbidden_Returns403Forbidden()
    {
        _notificationService.MarkAsReadException = new ForbiddenException("Forbidden");

        var result = await _controller.MarkAsRead(Guid.NewGuid(), CancellationToken.None);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task MarkAllAsRead_Returns204NoContent()
    {
        var result = await _controller.MarkAllAsRead(CancellationToken.None);

        var noContent = Assert.IsType<NoContentResult>(result);
        Assert.Equal(StatusCodes.Status204NoContent, noContent.StatusCode);
    }

    [Fact]
    public async Task MarkAllAsRead_WhenAuthException_Returns401Unauthorized()
    {
        _notificationService.MarkAllAsReadException = new AuthException("Not authenticated");

        var result = await _controller.MarkAllAsRead(CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    private sealed class FakeNotificationService : INotificationService
    {
        public PagedResult<NotificationResponse>? GetNotificationsResult { get; set; }
        public Exception? GetNotificationsException { get; set; }

        public NotificationResponse? MarkAsReadResult { get; set; }
        public Exception? MarkAsReadException { get; set; }

        public Exception? MarkAllAsReadException { get; set; }

        public Task<PagedResult<NotificationResponse>> GetNotificationsAsync(
            NotificationQueryParameters parameters,
            CancellationToken cancellationToken = default)
        {
            if (GetNotificationsException is not null)
            {
                throw GetNotificationsException;
            }

            return Task.FromResult(GetNotificationsResult ?? new PagedResult<NotificationResponse>());
        }

        public Task<NotificationResponse> MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (MarkAsReadException is not null)
            {
                throw MarkAsReadException;
            }

            return Task.FromResult(MarkAsReadResult ?? new NotificationResponse { Id = id });
        }

        public Task MarkAllAsReadAsync(CancellationToken cancellationToken = default)
        {
            if (MarkAllAsReadException is not null)
            {
                throw MarkAllAsReadException;
            }

            return Task.CompletedTask;
        }

        public Task CreateNotificationAsync(
            Guid userId,
            Guid? taskId,
            NotificationType type,
            string message,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}

