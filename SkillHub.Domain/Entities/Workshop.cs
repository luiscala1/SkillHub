using SkillHub.Domain.Enums;
using SkillHub.Domain.Exceptions;

namespace SkillHub.Domain.Entities;

public class Workshop
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public string InstructorId { get; set; } = string.Empty;
    public ApplicationUser Instructor { get; set; } = null!;

    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public int MaxCapacity { get; set; }

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    public int ActiveEnrollmentsCount => Enrollments.Count(e => e.Status == EnrollmentStatus.Active);

    public bool HasAvailableCapacity() => ActiveEnrollmentsCount < MaxCapacity;

    public void ValidateDates(DateTime utcNow)
    {
        if (EndDateTime <= StartDateTime)
        {
            throw new InvalidWorkshopDatesException("La fecha de finalización debe ser posterior a la fecha de inicio.");
        }

        if (StartDateTime <= utcNow)
        {
            throw new InvalidWorkshopDatesException("La fecha de inicio del taller debe ser en el futuro (mayor a la fecha y hora actual UTC).");
        }
    }

    public bool OverlapsWith(DateTime otherStart, DateTime otherEnd)
    {
        return (StartDateTime < otherEnd) && (EndDateTime > otherStart);
    }
}
