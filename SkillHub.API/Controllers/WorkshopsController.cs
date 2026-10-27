using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHub.Application.DTOs;
using SkillHub.Application.Services.Interfaces;
using SkillHub.Domain.Exceptions;

namespace SkillHub.API.Controllers;

[ApiController]
[Route("api/v1/workshops")]
public class WorkshopsController : ControllerBase
{
    private readonly IWorkshopService _workshopService;
    private readonly IEnrollmentService _enrollmentService;

    public WorkshopsController(
        IWorkshopService workshopService,
        IEnrollmentService enrollmentService)
    {
        _workshopService = workshopService;
        _enrollmentService = enrollmentService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetFutureWorkshops(CancellationToken cancellationToken)
    {
        var workshops = await _workshopService.GetFutureWorkshopsAsync(cancellationToken);
        return Ok(workshops);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetWorkshopById(Guid id, CancellationToken cancellationToken)
    {
        var workshop = await _workshopService.GetWorkshopByIdAsync(id, cancellationToken);
        if (workshop == null)
        {
            return NotFound(new { message = $"El taller con ID {id} no fue encontrado." });
        }

        return Ok(workshop);
    }

    [HttpPost]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> CreateWorkshop([FromBody] CreateWorkshopDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var workshop = await _workshopService.CreateWorkshopAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetWorkshopById), new { id = workshop.Id }, workshop);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}/enrollments")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> GetWorkshopEnrollments(Guid id, CancellationToken cancellationToken)
    {
        var enrollments = await _enrollmentService.GetEnrollmentsByWorkshopIdAsync(id, cancellationToken);
        return Ok(enrollments);
    }
}
