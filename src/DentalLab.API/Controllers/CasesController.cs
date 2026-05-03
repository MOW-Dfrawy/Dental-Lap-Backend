using DentalLab.API.Middleware;
using DentalLab.Application.Common;
using DentalLab.Application.DTOs;
using DentalLab.Application.Interfaces;
using DentalLab.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace DentalLab.API.Controllers;

[ApiController]
[Route("api/cases")]
public class CasesController : ControllerBase
{
    private readonly ICaseService _caseService;
    private readonly IWorkflowService _workflowService;

    public CasesController(ICaseService caseService, IWorkflowService workflowService)
    {
        _caseService = caseService;
        _workflowService = workflowService;
    }

    [HttpGet]
    [RequireRole(UserRole.Admin, UserRole.Reception, UserRole.Technician)]
    public async Task<ActionResult<PagedResult<CaseDto>>> Get([FromQuery] CaseFilterDto filter, CancellationToken ct)
        => Ok(await _caseService.GetCasesAsync(filter, ct));

    [HttpGet("{id:int}")]
    [RequireRole(UserRole.Admin, UserRole.Reception, UserRole.Technician)]
    public async Task<ActionResult<CaseDto>> GetById(int id, CancellationToken ct)
    {
        var c = await _caseService.GetCaseByIdAsync(id, ct);
        return c == null ? NotFound() : Ok(c);
    }

    [HttpGet("by-number/{caseNumber}")]
    [RequireRole(UserRole.Admin, UserRole.Reception, UserRole.Technician)]
    public async Task<ActionResult<CaseDto>> GetByNumber(string caseNumber, CancellationToken ct)
    {
        var c = await _caseService.GetCaseByNumberAsync(caseNumber, ct);
        return c == null ? NotFound() : Ok(c);
    }

    [HttpPost]
    [RequireRole(UserRole.Admin, UserRole.Reception)]
    public async Task<ActionResult<CaseDto>> Create([FromBody] CreateCaseDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var user = HttpContext.GetUserName();
        var created = await _caseService.CreateCaseAsync(dto, user, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [RequireRole(UserRole.Admin, UserRole.Reception, UserRole.Technician)]
    public async Task<ActionResult<CaseDto>> Update(int id, [FromBody] UpdateCaseDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var user = HttpContext.GetUserName();
        return Ok(await _caseService.UpdateCaseAsync(id, dto, user, ct));
    }

    [HttpDelete("{id:int}")]
    [RequireRole(UserRole.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var user = HttpContext.GetUserName();
        await _caseService.DeleteCaseAsync(id, user, ct);
        return NoContent();
    }

    // Workflow operations on a case
    [HttpPost("{id:int}/move-stage")]
    [RequireRole(UserRole.Admin, UserRole.Technician)]
    public async Task<ActionResult<CaseStageHistoryDto>> MoveStage(int id, [FromBody] MoveCaseToStageDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var user = HttpContext.GetUserName();
        return Ok(await _workflowService.MoveCaseToStageAsync(id, dto, user, ct));
    }

    [HttpGet("{id:int}/history")]
    [RequireRole(UserRole.Admin, UserRole.Reception, UserRole.Technician)]
    public async Task<ActionResult<IReadOnlyList<CaseStageHistoryDto>>> History(int id, CancellationToken ct)
        => Ok(await _workflowService.GetCaseHistoryAsync(id, ct));

    [HttpGet("{id:int}/total-duration")]
    [RequireRole(UserRole.Admin, UserRole.Reception, UserRole.Technician)]
    public async Task<ActionResult<object>> TotalDuration(int id, CancellationToken ct)
    {
        var minutes = await _workflowService.GetTotalDurationMinutesAsync(id, ct);
        return Ok(new { caseId = id, totalDurationMinutes = minutes, totalDurationHours = Math.Round(minutes / 60.0, 2) });
    }
}
