using SkillHub.Domain.Entities;

namespace SkillHub.Application.Common.Interfaces;

public interface IWorkshopRepository
{
    Task<Workshop?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Workshop?> GetByIdWithEnrollmentsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Workshop?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Workshop>> GetFutureWorkshopsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Workshop>> GetTopWorkshopsByActiveEnrollmentsAsync(int limit = 10, CancellationToken cancellationToken = default);
    Task AddAsync(Workshop workshop, CancellationToken cancellationToken = default);
    Task UpdateAsync(Workshop workshop, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
