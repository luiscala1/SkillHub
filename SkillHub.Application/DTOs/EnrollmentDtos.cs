using SkillHub.Domain.Enums;

namespace SkillHub.Application.DTOs;

public class EnrollmentDto
{
    public Guid Id { get; set; }
    public Guid WorkshopId { get; set; }
    public string WorkshopTitle { get; set; } = string.Empty;
    public string WorkshopCode { get; set; } = string.Empty;
    public string ParticipantId { get; set; } = string.Empty;
    public string? ParticipantName { get; set; }
    public EnrollmentStatus Status { get; set; }
    public DateTime EnrolledAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelledByUserId { get; set; }
}

public class EnrollParticipantRequestDto
{
    public Guid WorkshopId { get; set; }
    public string ParticipantId { get; set; } = string.Empty;
}
