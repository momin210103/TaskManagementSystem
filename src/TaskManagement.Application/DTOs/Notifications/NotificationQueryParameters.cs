namespace TaskManagement.Application.DTOs.Notifications;

public class NotificationQueryParameters
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public bool? IsRead { get; set; }

    public string? SortDirection { get; set; }
}

