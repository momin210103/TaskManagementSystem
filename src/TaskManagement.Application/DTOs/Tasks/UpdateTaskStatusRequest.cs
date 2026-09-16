using TaskManagement.Domain.Enums;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Application.DTOs.Tasks;

public class UpdateTaskStatusRequest
{
    public TaskStatus Status { get; set; }
}

