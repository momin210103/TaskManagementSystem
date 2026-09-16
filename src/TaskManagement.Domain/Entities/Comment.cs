using TaskManagement.Domain.Common;

namespace TaskManagement.Domain.Entities;

public class Comment : BaseEntity
{
    public Guid TaskId { get; set; }

    public Guid UserId { get; set; }

    public string Content { get; set; } = string.Empty;

    public TaskItem? Task { get; set; }
}