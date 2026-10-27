using SkillHub.Application.Common.Interfaces;
using SkillHub.Application.DTOs;
using SkillHub.Application.Services.Interfaces;
using SkillHub.Domain.Entities;
using SkillHub.Domain.Enums;
using SkillHub.Domain.Exceptions;

namespace SkillHub.Application.Services.Implementations;

public class EnrollmentService : IEnrollmentService
{
    private readonly IWorkshopRepository _workshopRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;

    public EnrollmentService(
        IWorkshopRepository workshopRepository,
        IEnrollmentRepository enrollmentRepository)
    {
        _workshopRepository = workshopRepository;
        _enrollmentRepository = enrollmentRepository;
    }

    public async Task<EnrollmentDto> EnrollParticipantAsync(
        EnrollParticipantRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var workshop = await _workshopRepository.GetByIdWithEnrollmentsAsync(request.WorkshopId, cancellationToken);
        if (workshop == null)
        {
            throw new DomainException($"El taller con ID {request.WorkshopId} no fue encontrado.");
        }

        var utcNow = DateTime.UtcNow;

        // Regla: Solo Futuros
        if (workshop.StartDateTime <= utcNow)
        {
            throw new DomainException("No es posible inscribirse a un taller que ya ha iniciado o pertenece al pasado.");
        }

        // Regla: Cupo Máximo
        if (!workshop.HasAvailableCapacity())
        {
            throw new WorkshopCapacityExceededException();
        }

        // Regla: Sin Duplicados / Reinscripción
        var existingEnrollment = await _enrollmentRepository.GetByWorkshopAndParticipantAsync(
            request.WorkshopId,
            request.ParticipantId,
            cancellationToken);

        if (existingEnrollment != null && existingEnrollment.Status == EnrollmentStatus.Active)
        {
            throw new DuplicateEnrollmentException();
        }

        // Regla: Cruce de Horarios
        var activeEnrollments = await _enrollmentRepository.GetActiveByParticipantAsync(
            request.ParticipantId,
            cancellationToken);

        foreach (var enrolled in activeEnrollments)
        {
            if (enrolled.WorkshopId != workshop.Id &&
                workshop.OverlapsWith(enrolled.Workshop.StartDateTime, enrolled.Workshop.EndDateTime))
            {
                throw new ScheduleConflictException(
                    $"El participante ya cuenta con el taller '{enrolled.Workshop.Title}' activo en un horario que se cruza con este taller.");
            }
        }

        Enrollment enrollment;

        // Regla: Reinscripción
        if (existingEnrollment != null && existingEnrollment.Status == EnrollmentStatus.Cancelled)
        {
            existingEnrollment.Reactivate(utcNow);
            await _enrollmentRepository.UpdateAsync(existingEnrollment, cancellationToken);
            enrollment = existingEnrollment;
        }
        else
        {
            enrollment = new Enrollment
            {
                Id = Guid.NewGuid(),
                WorkshopId = request.WorkshopId,
                ParticipantId = request.ParticipantId,
                Status = EnrollmentStatus.Active,
                EnrolledAt = utcNow
            };

            await _enrollmentRepository.AddAsync(enrollment, cancellationToken);
        }

        await _enrollmentRepository.SaveChangesAsync(cancellationToken);

        return new EnrollmentDto
        {
            Id = enrollment.Id,
            WorkshopId = workshop.Id,
            WorkshopCode = workshop.Code,
            WorkshopTitle = workshop.Title,
            ParticipantId = enrollment.ParticipantId,
            Status = enrollment.Status,
            EnrolledAt = enrollment.EnrolledAt,
            CancelledAt = enrollment.CancelledAt,
            CancelledByUserId = enrollment.CancelledByUserId
        };
    }

    public async Task CancelByParticipantAsync(
        Guid enrollmentId,
        string participantId,
        CancellationToken cancellationToken = default)
    {
        var enrollment = await _enrollmentRepository.GetByIdWithWorkshopAsync(enrollmentId, cancellationToken);
        if (enrollment == null)
        {
            throw new DomainException($"No se encontró la inscripción con ID {enrollmentId}.");
        }

        if (enrollment.ParticipantId != participantId)
        {
            throw new DomainException("No tiene permisos para cancelar una inscripción que no le pertenece.");
        }

        // Regla: Cancelación Participante (mínimo 24 horas de anticipación)
        enrollment.CancelByParticipant(DateTime.UtcNow);

        await _enrollmentRepository.UpdateAsync(enrollment, cancellationToken);
        await _enrollmentRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelByEmployeeAsync(
        Guid enrollmentId,
        string employeeUserId,
        CancellationToken cancellationToken = default)
    {
        var enrollment = await _enrollmentRepository.GetByIdWithWorkshopAsync(enrollmentId, cancellationToken);
        if (enrollment == null)
        {
            throw new DomainException($"No se encontró la inscripción con ID {enrollmentId}.");
        }

        // Regla: Cancelación Empleado (sin restricción de 24h, siempre que StartDateTime > UtcNow)
        enrollment.CancelByEmployee(employeeUserId, DateTime.UtcNow);

        await _enrollmentRepository.UpdateAsync(enrollment, cancellationToken);
        await _enrollmentRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EnrollmentDto>> GetEnrollmentsByWorkshopIdAsync(
        Guid workshopId,
        CancellationToken cancellationToken = default)
    {
        var enrollments = await _enrollmentRepository.GetByWorkshopIdAsync(workshopId, cancellationToken);
        return enrollments.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<EnrollmentDto>> GetMyEnrollmentsAsync(
        string participantId,
        CancellationToken cancellationToken = default)
    {
        var enrollments = await _enrollmentRepository.GetAllByParticipantAsync(participantId, cancellationToken);
        return enrollments.Select(MapToDto).ToList();
    }

    private static EnrollmentDto MapToDto(Enrollment enrollment)
    {
        return new EnrollmentDto
        {
            Id = enrollment.Id,
            WorkshopId = enrollment.WorkshopId,
            WorkshopCode = enrollment.Workshop?.Code ?? string.Empty,
            WorkshopTitle = enrollment.Workshop?.Title ?? string.Empty,
            ParticipantId = enrollment.ParticipantId,
            ParticipantName = enrollment.Participant?.FullName,
            Status = enrollment.Status,
            EnrolledAt = enrollment.EnrolledAt,
            CancelledAt = enrollment.CancelledAt,
            CancelledByUserId = enrollment.CancelledByUserId
        };
    }
}
