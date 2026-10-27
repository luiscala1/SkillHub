using Microsoft.AspNetCore.Identity;

namespace SkillHub.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? Specialty { get; set; }
    public string Role { get; set; } = string.Empty;

    public ICollection<Workshop> InstructedWorkshops { get; set; } = new List<Workshop>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
