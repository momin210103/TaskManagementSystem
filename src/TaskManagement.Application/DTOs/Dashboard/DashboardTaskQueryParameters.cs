namespace TaskManagement.Application.DTOs.Dashboard;

public class DashboardTaskQueryParameters
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string? Status { get; set; }

    public string? Priority { get; set; }

    public Guid? TeamId { get; set; }

    public Guid? AssignedToId { get; set; }

    public DateOnly? DeadlineFrom { get; set; }

    public DateOnly? DeadlineTo { get; set; }

    public DateOnly? Deadline { get; set; }

    public string? SortBy { get; set; }

    public string? SortDirection { get; set; }
}

