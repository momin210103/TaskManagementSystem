using Microsoft.AspNetCore.Identity;

namespace TaskManagement.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string Name { get; set; } = string.Empty;

    public Guid? TeamId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

