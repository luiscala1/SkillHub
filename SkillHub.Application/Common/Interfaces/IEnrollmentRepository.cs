using SkillHub.Domain.Entities;

namespace SkillHub.Application.Common.Interfaces;

public interface IEnrollmentRepository
{
    Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Enrollment?> GetByIdWithWorkshopAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Enrollment?> GetByWorkshopAndParticipantAsync(Guid workshopId, string participantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Enrollment>> GetByWorkshopIdAsync(Guid workshopId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Enrollment>> GetAllByParticipantAsync(string participantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Enrollment>> GetActiveByParticipantAsync(string participantId, CancellationToken cancellationToken = default);
    Task AddAsync(Enrollment enrollment, CancellationToken cancellationToken = default);
    Task UpdateAsync(Enrollment enrollment, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
