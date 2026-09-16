using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.API.Controllers;
using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using Xunit;

namespace TaskManagement.UnitTests;

public sealed class TasksControllerTests
{
    private readonly MockTaskService _mockTaskService = new();
    private readonly TasksController _controller;

    public TasksControllerTests()
    {
        _controller = new TasksController(_mockTaskService);
    }

    [Fact]
    public async Task CreateTask_ReturnsCreated_WhenSuccessful()
    {
        var taskId = Guid.NewGuid();
        _mockTaskService.CreateTaskResult = new TaskResponse { Id = taskId, Title = "Task 1" };

        var response = await _controller.CreateTask(new CreateTaskRequest { Title = "Task 1" }, CancellationToken.None);

        var createdAtResult = Assert.IsType<CreatedAtActionResult>(response);
        var result = Assert.IsType<TaskResponse>(createdAtResult.Value);
        Assert.Equal(taskId, result.Id);
    }

    [Fact]
    public async Task CreateTask_ReturnsBadRequest_OnValidationException()
    {
        _mockTaskService.ExceptionToThrow = new ValidationException("Title required");

        var response = await _controller.CreateTask(new CreateTaskRequest(), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(response);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
    }

    [Fact]
    public async Task CreateTask_ReturnsForbidden_OnForbiddenException()
    {
        _mockTaskService.ExceptionToThrow = new ForbiddenException("Denied");

        var response = await _controller.CreateTask(new CreateTaskRequest(), CancellationToken.None);

        var forbidden = Assert.IsType<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task GetTasks_ReturnsOk_WithPagedResult()
    {
        _mockTaskService.GetTasksResult = new PagedResult<TaskResponse>
        {
            Items = [new TaskResponse { Id = Guid.NewGuid() }],
            TotalCount = 1
        };

        var response = await _controller.GetTasks(new TaskQueryParameters(), CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response);
        var paged = Assert.IsType<PagedResult<TaskResponse>>(okResult.Value);
        Assert.Equal(1, paged.TotalCount);
    }

    [Fact]
    public async Task GetTaskById_ReturnsOk_WhenFound()
    {
        var taskId = Guid.NewGuid();
        _mockTaskService.GetTaskByIdResult = new TaskResponse { Id = taskId, Title = "Task 1" };

        var response = await _controller.GetTaskById(taskId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response);
        var task = Assert.IsType<TaskResponse>(okResult.Value);
        Assert.Equal(taskId, task.Id);
    }

    [Fact]
    public async Task GetTaskById_ReturnsNotFound_WhenNotFound()
    {
        _mockTaskService.ExceptionToThrow = new NotFoundException("Task not found");

        var response = await _controller.GetTaskById(Guid.NewGuid(), CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(response);
        var problem = Assert.IsType<ProblemDetails>(notFound.Value);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
    }

    [Fact]
    public async Task UpdateTask_ReturnsOk_WhenSuccessful()
    {
        var taskId = Guid.NewGuid();
        _mockTaskService.UpdateTaskResult = new TaskResponse { Id = taskId, Title = "Updated" };

        var response = await _controller.UpdateTask(taskId, new UpdateTaskRequest { Title = "Updated" }, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response);
        var task = Assert.IsType<TaskResponse>(okResult.Value);
        Assert.Equal("Updated", task.Title);
    }

    [Fact]
    public async Task UpdateTaskStatus_ReturnsOk_WhenSuccessful()
    {
        var taskId = Guid.NewGuid();
        _mockTaskService.UpdateTaskStatusResult = new TaskResponse { Id = taskId, Status = "Done" };

        var response = await _controller.UpdateTaskStatus(taskId, new UpdateTaskStatusRequest(), CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response);
        var task = Assert.IsType<TaskResponse>(okResult.Value);
        Assert.Equal("Done", task.Status);
    }

    [Fact]
    public async Task AssignTask_ReturnsOk_WhenSuccessful()
    {
        var taskId = Guid.NewGuid();
        _mockTaskService.AssignTaskResult = new TaskResponse { Id = taskId, AssignedToName = "Alice" };

        var response = await _controller.AssignTask(taskId, new AssignTaskRequest { AssignedToId = Guid.NewGuid() }, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response);
        var task = Assert.IsType<TaskResponse>(okResult.Value);
        Assert.Equal("Alice", task.AssignedToName);
    }

    [Fact]
    public async Task DeleteTask_ReturnsNoContent_WhenSuccessful()
    {
        var response = await _controller.DeleteTask(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NoContentResult>(response);
    }

    private sealed class MockTaskService : ITaskService
    {
        public Exception? ExceptionToThrow { get; set; }
        public TaskResponse? CreateTaskResult { get; set; }
        public PagedResult<TaskResponse> GetTasksResult { get; set; } = new();
        public TaskResponse? GetTaskByIdResult { get; set; }
        public TaskResponse? UpdateTaskResult { get; set; }
        public TaskResponse? UpdateTaskStatusResult { get; set; }
        public TaskResponse? AssignTaskResult { get; set; }

        public Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request, CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }
            return Task.FromResult(CreateTaskResult ?? new TaskResponse());
        }

        public Task<PagedResult<TaskResponse>> GetTasksAsync(TaskQueryParameters parameters, CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }
            return Task.FromResult(GetTasksResult);
        }

        public Task<TaskResponse> GetTaskByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }
            return Task.FromResult(GetTaskByIdResult ?? new TaskResponse());
        }

        public Task<TaskResponse> UpdateTaskAsync(Guid id, UpdateTaskRequest request, CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }
            return Task.FromResult(UpdateTaskResult ?? new TaskResponse());
        }

        public Task<TaskResponse> UpdateTaskStatusAsync(Guid id, UpdateTaskStatusRequest request, CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }
            return Task.FromResult(UpdateTaskStatusResult ?? new TaskResponse());
        }

        public Task<TaskResponse> AssignTaskAsync(Guid id, AssignTaskRequest request, CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }
            return Task.FromResult(AssignTaskResult ?? new TaskResponse());
        }

        public Task DeleteTaskAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }
            return Task.CompletedTask;
        }
    }
}

