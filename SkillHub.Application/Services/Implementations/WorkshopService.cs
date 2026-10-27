using SkillHub.Application.Common.Interfaces;
using SkillHub.Application.DTOs;
using SkillHub.Application.Services.Interfaces;
using SkillHub.Domain.Entities;
using SkillHub.Domain.Exceptions;

namespace SkillHub.Application.Services.Implementations;

public class WorkshopService : IWorkshopService
{
    private readonly IWorkshopRepository _workshopRepository;

    public WorkshopService(IWorkshopRepository workshopRepository)
    {
        _workshopRepository = workshopRepository;
    }

    public async Task<WorkshopDto> CreateWorkshopAsync(CreateWorkshopDto dto, CancellationToken cancellationToken = default)
    {
        if (await _workshopRepository.CodeExistsAsync(dto.Code, cancellationToken))
        {
            throw new DomainException($"Ya existe un taller registrado con el código '{dto.Code}'.");
        }

        var workshop = new Workshop
        {
            Id = Guid.NewGuid(),
            Code = dto.Code.Trim(),
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            InstructorId = dto.InstructorId,
            StartDateTime = dto.StartDateTime,
            EndDateTime = dto.EndDateTime,
            MaxCapacity = dto.MaxCapacity
        };

        // Reglas de fechas a nivel de entidad
        workshop.ValidateDates(DateTime.UtcNow);

        await _workshopRepository.AddAsync(workshop, cancellationToken);
        await _workshopRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(workshop);
    }

    public async Task<WorkshopDto?> GetWorkshopByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var workshop = await _workshopRepository.GetByIdWithEnrollmentsAsync(id, cancellationToken);
        return workshop == null ? null : MapToDto(workshop);
    }

    public async Task<IReadOnlyList<WorkshopDto>> GetFutureWorkshopsAsync(CancellationToken cancellationToken = default)
    {
        var workshops = await _workshopRepository.GetFutureWorkshopsAsync(cancellationToken);
        return workshops.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<WorkshopDto>> GetTopWorkshopsAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        var workshops = await _workshopRepository.GetTopWorkshopsByActiveEnrollmentsAsync(limit, cancellationToken);
        return workshops.Select(MapToDto).ToList();
    }

    private static WorkshopDto MapToDto(Workshop workshop)
    {
        return new WorkshopDto
        {
            Id = workshop.Id,
            Code = workshop.Code,
            Title = workshop.Title,
            Description = workshop.Description,
            InstructorId = workshop.InstructorId,
            InstructorName = workshop.Instructor?.FullName,
            StartDateTime = workshop.StartDateTime,
            EndDateTime = workshop.EndDateTime,
            MaxCapacity = workshop.MaxCapacity,
            ActiveEnrollmentsCount = workshop.ActiveEnrollmentsCount,
            HasAvailableCapacity = workshop.HasAvailableCapacity()
        };
    }
}
