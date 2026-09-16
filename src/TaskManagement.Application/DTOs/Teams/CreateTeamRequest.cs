namespace TaskManagement.Application.DTOs.Teams;

public class CreateTeamRequest
{
    public string Name { get; set; } = string.Empty;

    public Guid ManagerId { get; set; }
}

