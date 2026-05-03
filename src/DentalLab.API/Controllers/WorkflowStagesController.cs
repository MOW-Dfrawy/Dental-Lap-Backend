using DentalLab.API.Middleware;
using DentalLab.Application.DTOs;
using DentalLab.Application.Interfaces;
using DentalLab.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace DentalLab.API.Controllers;

[ApiController]
[Route("api/workflow-stages")]
public class WorkflowStagesController : ControllerBase
{
    private readonly IWorkflowService _service;

    public WorkflowStagesController(IWorkflowService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequireRole(UserRole.Admin, UserRole.Reception, UserRole.Technician)]
    public async Task<ActionResult<IReadOnlyList<WorkflowStageDto>>> Get(CancellationToken ct)
        => Ok(await _service.GetStagesAsync(ct));

    [HttpGet("{id:int}")]
    [RequireRole(UserRole.Admin, UserRole.Reception, UserRole.Technician)]
    public async Task<ActionResult<WorkflowStageDto>> GetById(int id, CancellationToken ct)
    {
        var s = await _service.GetStageByIdAsync(id, ct);
        return s == null ? NotFound() : Ok(s);
    }

    [HttpPost]
    [RequireRole(UserRole.Admin)]
    public async Task<ActionResult<WorkflowStageDto>> Create([FromBody] CreateWorkflowStageDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var created = await _service.CreateStageAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [RequireRole(UserRole.Admin)]
    public async Task<ActionResult<WorkflowStageDto>> Update(int id, [FromBody] UpdateWorkflowStageDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return Ok(await _service.UpdateStageAsync(id, dto, ct));
    }

    [HttpDelete("{id:int}")]
    [RequireRole(UserRole.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeleteStageAsync(id, ct);
        return NoContent();
    }
}
