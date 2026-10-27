using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHub.Application.Services.Interfaces;

namespace SkillHub.API.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize(Roles = "Employee")]
public class ReportsController : ControllerBase
{
    private readonly IWorkshopService _workshopService;

    public ReportsController(IWorkshopService workshopService)
    {
        _workshopService = workshopService;
    }

    [HttpGet("top-workshops")]
    public async Task<IActionResult> GetTopWorkshops([FromQuery] int limit = 10, CancellationToken cancellationToken = default)
    {
        var topWorkshops = await _workshopService.GetTopWorkshopsAsync(limit, cancellationToken);
        return Ok(topWorkshops);
    }
}
