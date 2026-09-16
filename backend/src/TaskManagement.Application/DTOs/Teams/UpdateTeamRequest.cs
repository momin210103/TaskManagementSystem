namespace TaskManagement.Application.DTOs.Teams;

public class UpdateTeamRequest
{
    public string? Name { get; set; }

    public Guid? ManagerId { get; set; }
}

