namespace TaskManagement.Application.DTOs.Users;

public class UserQueryParameters
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public Guid? TeamId { get; set; }

    public string? Role { get; set; }
}

