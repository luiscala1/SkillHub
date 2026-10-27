using SkillHub.Application.DTOs;

namespace SkillHub.Application.Services.Interfaces;

public interface IExcelImportService
{
    Task<ExcelImportResultDto> ImportEnrollmentsFromExcelAsync(Stream fileStream, CancellationToken cancellationToken = default);
}
