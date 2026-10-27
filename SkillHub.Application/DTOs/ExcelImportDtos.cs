namespace SkillHub.Application.DTOs;

public class ExcelRowErrorDto
{
    public int RowNumber { get; set; }
    public string? Identifier { get; set; } // ParticipantEmail or WorkshopCode
    public string ErrorMessage { get; set; } = string.Empty;
}

public class ImportedEnrollmentDto
{
    public int RowNumber { get; set; }
    public string ParticipantEmail { get; set; } = string.Empty;
    public string WorkshopCode { get; set; } = string.Empty;
    public string WorkshopTitle { get; set; } = string.Empty;
    public Guid EnrollmentId { get; set; }
}

public class ExcelImportResultDto
{
    public int TotalRowsProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount => Errors.Count;
    public List<ExcelRowErrorDto> Errors { get; set; } = new();
    public List<ImportedEnrollmentDto> ImportedEnrollments { get; set; } = new();
}
