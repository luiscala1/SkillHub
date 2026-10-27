using SkillHub.Application.DTOs;

namespace SkillHub.Application.Services.Interfaces;

public interface IWorkshopService
{
    Task<WorkshopDto> CreateWorkshopAsync(CreateWorkshopDto dto, CancellationToken cancellationToken = default);
    Task<WorkshopDto?> GetWorkshopByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkshopDto>> GetFutureWorkshopsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkshopDto>> GetTopWorkshopsAsync(int limit = 10, CancellationToken cancellationToken = default);
}
