using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHub.Application.Common.Interfaces;
using SkillHub.Application.DTOs;
using SkillHub.Application.Services.Interfaces;
using SkillHub.Domain.Exceptions;

namespace SkillHub.API.Controllers;

[ApiController]
[Route("api/v1/enrollments")]
public class EnrollmentsController : ControllerBase
{
    private readonly IEnrollmentService _enrollmentService;
    private readonly IExcelImportService _excelImportService;
    private readonly IPdfService _pdfService;
    private readonly IEnrollmentRepository _enrollmentRepository;

    public EnrollmentsController(
        IEnrollmentService enrollmentService,
        IExcelImportService excelImportService,
        IPdfService pdfService,
        IEnrollmentRepository enrollmentRepository)
    {
        _enrollmentService = enrollmentService;
        _excelImportService = excelImportService;
        _pdfService = pdfService;
        _enrollmentRepository = enrollmentRepository;
    }

    [HttpPost]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> Enroll([FromBody] EnrollParticipantRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            request.ParticipantId = userId;

            var result = await _enrollmentService.EnrollParticipantAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("mine")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> GetMyEnrollments(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var enrollments = await _enrollmentService.GetMyEnrollmentsAsync(userId, cancellationToken);
        return Ok(enrollments);
    }

    [HttpPatch("{id:guid}/cancel")]
    [Authorize]
    public async Task<IActionResult> CancelEnrollment(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var isEmployee = User.IsInRole("Employee");

            var enrollment = await _enrollmentRepository.GetByIdAsync(id, cancellationToken);
            if (enrollment == null)
            {
                return NotFound(new { message = $"No se encontró la inscripción con ID {id}." });
            }

            if (!isEmployee && enrollment.ParticipantId != userId)
            {
                return Forbid();
            }

            if (isEmployee)
            {
                await _enrollmentService.CancelByEmployeeAsync(id, userId, cancellationToken);
            }
            else
            {
                await _enrollmentService.CancelByParticipantAsync(id, userId, cancellationToken);
            }

            return Ok(new { message = "Inscripción cancelada exitosamente." });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("import")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> ImportEnrollments(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Debe proporcionar un archivo Excel (.xlsx) válido." });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var result = await _excelImportService.ImportEnrollmentsFromExcelAsync(stream, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Error al procesar el archivo Excel: {ex.Message}" });
        }
    }

    [HttpGet("{id:guid}/certificate")]
    [Authorize]
    public async Task<IActionResult> DownloadCertificate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var isEmployee = User.IsInRole("Employee");

            var enrollment = await _enrollmentRepository.GetByIdAsync(id, cancellationToken);
            if (enrollment == null)
            {
                return NotFound(new { message = $"No se encontró la inscripción con ID {id}." });
            }

            if (!isEmployee && enrollment.ParticipantId != userId)
            {
                return Forbid();
            }

            var pdfBytes = await _pdfService.GenerateEnrollmentCertificatePdfAsync(id, cancellationToken);
            return File(pdfBytes, "application/pdf", $"comprobante-{id}.pdf");
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) 
               ?? User.FindFirstValue("sub") 
               ?? throw new UnauthorizedAccessException("Usuario no autenticado.");
    }
}
