using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.API.Controllers;
using TaskManagement.Application.DTOs.Comments;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using Xunit;

namespace TaskManagement.UnitTests;

public sealed class CommentsControllerTests
{
    private readonly MockCommentService _mockCommentService = new();
    private readonly CommentsController _controller;

    public CommentsControllerTests()
    {
        _controller = new CommentsController(_mockCommentService);
    }

    [Fact]
    public async Task CreateComment_ReturnsCreated_WhenSuccessful()
    {
        var taskId = Guid.NewGuid();
        var commentId = Guid.NewGuid();
        _mockCommentService.CreateCommentResult = new CommentResponse { Id = commentId, TaskId = taskId, Content = "Hello" };

        var response = await _controller.CreateComment(taskId, new CreateCommentRequest { Content = "Hello" }, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
        var result = Assert.IsType<CommentResponse>(objectResult.Value);
        Assert.Equal(commentId, result.Id);
    }

    [Fact]
    public async Task CreateComment_ReturnsBadRequest_OnValidationException()
    {
        _mockCommentService.ExceptionToThrow = new ValidationException("Content required");

        var response = await _controller.CreateComment(Guid.NewGuid(), new CreateCommentRequest(), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(response);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
    }

    [Fact]
    public async Task CreateComment_ReturnsNotFound_OnNotFoundException()
    {
        _mockCommentService.ExceptionToThrow = new NotFoundException("Task not found");

        var response = await _controller.CreateComment(Guid.NewGuid(), new CreateCommentRequest { Content = "Hello" }, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(response);
        var problem = Assert.IsType<ProblemDetails>(notFound.Value);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
    }

    [Fact]
    public async Task CreateComment_ReturnsForbidden_OnForbiddenException()
    {
        _mockCommentService.ExceptionToThrow = new ForbiddenException("Denied");

        var response = await _controller.CreateComment(Guid.NewGuid(), new CreateCommentRequest { Content = "Hello" }, CancellationToken.None);

        var forbidden = Assert.IsType<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task GetComments_ReturnsOk_WithCommentsList()
    {
        var taskId = Guid.NewGuid();
        _mockCommentService.GetCommentsResult = [new CommentResponse { Id = Guid.NewGuid(), TaskId = taskId, Content = "Test" }];

        var response = await _controller.GetComments(taskId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response);
        var comments = Assert.IsAssignableFrom<IReadOnlyList<CommentResponse>>(okResult.Value);
        Assert.Single(comments);
    }

    [Fact]
    public async Task GetComments_ReturnsNotFound_OnNotFoundException()
    {
        _mockCommentService.ExceptionToThrow = new NotFoundException("Task not found");

        var response = await _controller.GetComments(Guid.NewGuid(), CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(response);
        var problem = Assert.IsType<ProblemDetails>(notFound.Value);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
    }

    [Fact]
    public async Task DeleteComment_ReturnsNoContent_WhenSuccessful()
    {
        var response = await _controller.DeleteComment(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NoContentResult>(response);
    }

    [Fact]
    public async Task DeleteComment_ReturnsForbidden_OnForbiddenException()
    {
        _mockCommentService.ExceptionToThrow = new ForbiddenException("Denied");

        var response = await _controller.DeleteComment(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        var forbidden = Assert.IsType<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
    }

    private sealed class MockCommentService : ICommentService
    {
        public Exception? ExceptionToThrow { get; set; }
        public CommentResponse? CreateCommentResult { get; set; }
        public IReadOnlyList<CommentResponse> GetCommentsResult { get; set; } = [];

        public Task<CommentResponse> CreateCommentAsync(Guid taskId, CreateCommentRequest request, CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }
            return Task.FromResult(CreateCommentResult ?? new CommentResponse());
        }

        public Task<IReadOnlyList<CommentResponse>> GetCommentsAsync(Guid taskId, CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }
            return Task.FromResult(GetCommentsResult);
        }

        public Task DeleteCommentAsync(Guid taskId, Guid commentId, CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }
            return Task.CompletedTask;
        }
    }
}

