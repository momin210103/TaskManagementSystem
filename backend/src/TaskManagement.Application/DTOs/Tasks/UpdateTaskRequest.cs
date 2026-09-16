using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.DTOs.Tasks;

public class UpdateTaskRequest
{
    public string? Title { get; set; }

    public string? Description { get; set; }

    public TaskPriority? Priority { get; set; }

    public DateOnly? Deadline { get; set; }
}

