using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs.Tasks;

namespace TaskManagement.Application.Interfaces;

public interface ITaskService
{
    Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<TaskResponse>> GetTasksAsync(TaskQueryParameters parameters, CancellationToken cancellationToken = default);

    Task<TaskResponse> GetTaskByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TaskResponse> UpdateTaskAsync(Guid id, UpdateTaskRequest request, CancellationToken cancellationToken = default);

    Task<TaskResponse> UpdateTaskStatusAsync(Guid id, UpdateTaskStatusRequest request, CancellationToken cancellationToken = default);

    Task<TaskResponse> AssignTaskAsync(Guid id, AssignTaskRequest request, CancellationToken cancellationToken = default);

    Task DeleteTaskAsync(Guid id, CancellationToken cancellationToken = default);
}

