namespace TaskManagement.Application.DTOs.Tasks;

public class TaskDetailsResponse
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;

    public DateOnly Deadline { get; set; }

    public Guid TeamId { get; set; }

    public string TeamName { get; set; } = string.Empty;

    public Guid AssignedToId { get; set; }

    public string AssignedToName { get; set; } = string.Empty;

    public Guid AssignedById { get; set; }

    public string AssignedByName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

