namespace TaskManagement.Application.DTOs.Teams;

public class TeamDetailsResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid ManagerId { get; set; }

    public string ManagerName { get; set; } = string.Empty;

    public string ManagerEmail { get; set; } = string.Empty;

    public IReadOnlyList<TeamMemberResponse> Members { get; set; } = [];

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

