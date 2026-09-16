using TaskManagement.Domain.Common;

namespace TaskManagement.Domain.Entities;

public class Team : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public Guid ManagerId { get; set; }

    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}