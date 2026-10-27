using SkillHub.Domain.Enums;
using SkillHub.Domain.Exceptions;

namespace SkillHub.Domain.Entities;

public class Enrollment
{
    public Guid Id { get; set; }

    public Guid WorkshopId { get; set; }
    public Workshop Workshop { get; set; } = null!;

    public string ParticipantId { get; set; } = string.Empty;
    public ApplicationUser Participant { get; set; } = null!;

    public EnrollmentStatus Status { get; set; }

    public DateTime EnrolledAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public string? CancelledByUserId { get; set; }
    public ApplicationUser? CancelledByUser { get; set; }

    public void CancelByParticipant(DateTime utcNow)
    {
        if (Status != EnrollmentStatus.Active)
        {
            throw new EnrollmentCancellationNotAllowedException("La inscripción ya se encuentra cancelada.");
        }

        if (Workshop == null)
        {
            throw new InvalidOperationException("Se requiere la información del taller asociado para validar la cancelación.");
        }

        if (Workshop.StartDateTime - utcNow < TimeSpan.FromHours(24))
        {
            throw new EnrollmentCancellationNotAllowedException(
                "Los participantes solo pueden cancelar su inscripción con al menos 24 horas de anticipación al inicio del taller.");
        }

        Status = EnrollmentStatus.Cancelled;
        CancelledAt = utcNow;
        CancelledByUserId = ParticipantId;
    }

    public void CancelByEmployee(string employeeUserId, DateTime utcNow)
    {
        if (Status != EnrollmentStatus.Active)
        {
            throw new EnrollmentCancellationNotAllowedException("La inscripción ya se encuentra cancelada.");
        }

        if (Workshop == null)
        {
            throw new InvalidOperationException("Se requiere la información del taller asociado para validar la cancelación.");
        }

        if (Workshop.StartDateTime <= utcNow)
        {
            throw new EnrollmentCancellationNotAllowedException(
                "No es posible cancelar la inscripción de un taller que ya ha iniciado o finalizado.");
        }

        Status = EnrollmentStatus.Cancelled;
        CancelledAt = utcNow;
        CancelledByUserId = employeeUserId;
    }

    public void Reactivate(DateTime utcNow)
    {
        Status = EnrollmentStatus.Active;
        CancelledAt = null;
        CancelledByUserId = null;
        EnrolledAt = utcNow;
    }
}
