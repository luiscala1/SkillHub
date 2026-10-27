using SkillHub.Application.DTOs;

namespace SkillHub.Application.Services.Interfaces;

public interface IEnrollmentService
{
    Task<EnrollmentDto> EnrollParticipantAsync(EnrollParticipantRequestDto request, CancellationToken cancellationToken = default);
    Task CancelByParticipantAsync(Guid enrollmentId, string participantId, CancellationToken cancellationToken = default);
    Task CancelByEmployeeAsync(Guid enrollmentId, string employeeUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EnrollmentDto>> GetEnrollmentsByWorkshopIdAsync(Guid workshopId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EnrollmentDto>> GetMyEnrollmentsAsync(string participantId, CancellationToken cancellationToken = default);
}
