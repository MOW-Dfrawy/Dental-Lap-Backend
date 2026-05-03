using DentalLab.API.Middleware;
using DentalLab.Application.Common;
using DentalLab.Application.DTOs;
using DentalLab.Application.Interfaces;
using DentalLab.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace DentalLab.API.Controllers;

[ApiController]
[Route("api/patients")]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _service;

    public PatientsController(IPatientService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequireRole(UserRole.Admin, UserRole.Reception, UserRole.Technician)]
    public async Task<ActionResult<PagedResult<PatientDto>>> Get(
        [FromQuery] QueryParameters parameters,
        [FromQuery] int? clinicId,
        CancellationToken ct)
        => Ok(await _service.GetAsync(parameters, clinicId, ct));

    [HttpGet("{id:int}")]
    [RequireRole(UserRole.Admin, UserRole.Reception, UserRole.Technician)]
    public async Task<ActionResult<PatientDto>> GetById(int id, CancellationToken ct)
    {
        var p = await _service.GetByIdAsync(id, ct);
        return p == null ? NotFound() : Ok(p);
    }

    [HttpPost]
    [RequireRole(UserRole.Admin, UserRole.Reception)]
    public async Task<ActionResult<PatientDto>> Create([FromBody] CreatePatientDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var created = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [RequireRole(UserRole.Admin, UserRole.Reception)]
    public async Task<ActionResult<PatientDto>> Update(int id, [FromBody] UpdatePatientDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return Ok(await _service.UpdateAsync(id, dto, ct));
    }

    [HttpDelete("{id:int}")]
    [RequireRole(UserRole.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
