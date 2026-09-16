using TaskManagement.Domain.Common;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }

    public Guid? TaskId { get; set; }

    public NotificationType Type { get; set; } = NotificationType.Assignment;

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public TaskItem? Task { get; set; }
}