namespace SkillHub.Application.DTOs;

public class WorkshopDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string InstructorId { get; set; } = string.Empty;
    public string? InstructorName { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public int MaxCapacity { get; set; }
    public int ActiveEnrollmentsCount { get; set; }
    public int AvailableSpots => Math.Max(0, MaxCapacity - ActiveEnrollmentsCount);
    public bool HasAvailableCapacity { get; set; }
}

public class CreateWorkshopDto
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string InstructorId { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public int MaxCapacity { get; set; }
}
