using ClosedXML.Excel;
using SkillHub.Application.Common.Interfaces;
using SkillHub.Application.DTOs;
using SkillHub.Application.Services.Interfaces;

namespace SkillHub.Application.Services.Implementations;

public class ExcelImportService : IExcelImportService
{
    private readonly IUserRepository _userRepository;
    private readonly IWorkshopRepository _workshopRepository;
    private readonly IEnrollmentService _enrollmentService;

    public ExcelImportService(
        IUserRepository userRepository,
        IWorkshopRepository workshopRepository,
        IEnrollmentService enrollmentService)
    {
        _userRepository = userRepository;
        _workshopRepository = workshopRepository;
        _enrollmentService = enrollmentService;
    }

    public async Task<ExcelImportResultDto> ImportEnrollmentsFromExcelAsync(
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        var result = new ExcelImportResultDto();

        using var workbook = new XLWorkbook(fileStream);

        // Buscar hoja "Enrollments" o tomar la primera hoja activa
        IXLWorksheet? worksheet = null;
        if (workbook.TryGetWorksheet("Enrollments", out var enrollmentsSheet))
        {
            worksheet = enrollmentsSheet;
        }
        else
        {
            worksheet = workbook.Worksheets.FirstOrDefault();
        }

        if (worksheet == null)
        {
            result.Errors.Add(new ExcelRowErrorDto
            {
                RowNumber = 0,
                ErrorMessage = "El archivo Excel no contiene hojas de trabajo válidas."
            });
            return result;
        }

        var rows = worksheet.RowsUsed().Skip(1); // Saltar encabezado (ParticipantEmail, WorkshopCode)

        foreach (var row in rows)
        {
            result.TotalRowsProcessed++;
            var rowNumber = row.RowNumber();
            string? participantEmail = null;
            string? workshopCode = null;

            try
            {
                participantEmail = row.Cell(1).GetString()?.Trim();
                workshopCode = row.Cell(2).GetString()?.Trim();

                if (string.IsNullOrWhiteSpace(participantEmail))
                {
                    throw new InvalidOperationException("El correo del participante es obligatorio.");
                }

                if (string.IsNullOrWhiteSpace(workshopCode))
                {
                    throw new InvalidOperationException("El código del taller es obligatorio.");
                }

                // 1. Validar existencia del participante
                var participant = await _userRepository.GetByEmailAsync(participantEmail, cancellationToken);
                if (participant == null)
                {
                    throw new InvalidOperationException($"No existe un participante registrado con el correo '{participantEmail}'.");
                }

                // 2. Validar existencia del taller
                var workshop = await _workshopRepository.GetByCodeAsync(workshopCode, cancellationToken);
                if (workshop == null)
                {
                    throw new InvalidOperationException($"No existe un taller registrado con el código '{workshopCode}'.");
                }

                // 3. Procesar inscripción ejecutando la matriz completa de reglas de negocio
                // (Solo futuros, cupo máximo, sin duplicados, cruce de horarios, reinscripción si cancelado)
                var enrollmentDto = await _enrollmentService.EnrollParticipantAsync(new EnrollParticipantRequestDto
                {
                    WorkshopId = workshop.Id,
                    ParticipantId = participant.Id
                }, cancellationToken);

                result.SuccessCount++;
                result.ImportedEnrollments.Add(new ImportedEnrollmentDto
                {
                    RowNumber = rowNumber,
                    ParticipantEmail = participantEmail,
                    WorkshopCode = workshopCode,
                    WorkshopTitle = workshop.Title,
                    EnrollmentId = enrollmentDto.Id
                });
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ExcelRowErrorDto
                {
                    RowNumber = rowNumber,
                    Identifier = $"{participantEmail} | {workshopCode}",
                    ErrorMessage = ex.Message
                });
            }
        }

        return result;
    }
}
