using TaskManagement.Domain.Common;
using TaskManagement.Domain.Enums;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Domain.Entities;

public class TaskItem : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TaskStatus Status { get; set; } = TaskStatus.ToDo;

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public DateOnly Deadline { get; set; }

    public Guid TeamId { get; set; }

    public Guid AssignedToId { get; set; }

    public Guid AssignedById { get; set; }

    public Team? Team { get; set; }

    public ICollection<Comment> Comments { get; } = new List<Comment>();
}