using DentalLab.API.Middleware;
using DentalLab.Application.Common;
using DentalLab.Application.DTOs;
using DentalLab.Application.Interfaces;
using DentalLab.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace DentalLab.API.Controllers;

[ApiController]
[Route("api/clinics")]
public class ClinicsController : ControllerBase
{
    private readonly IClinicService _service;

    public ClinicsController(IClinicService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequireRole(UserRole.Admin, UserRole.Reception, UserRole.Technician)]
    public async Task<ActionResult<PagedResult<ClinicDto>>> Get([FromQuery] QueryParameters parameters, CancellationToken ct)
        => Ok(await _service.GetAsync(parameters, ct));

    [HttpGet("{id:int}")]
    [RequireRole(UserRole.Admin, UserRole.Reception, UserRole.Technician)]
    public async Task<ActionResult<ClinicDto>> GetById(int id, CancellationToken ct)
    {
        var clinic = await _service.GetByIdAsync(id, ct);
        return clinic == null ? NotFound() : Ok(clinic);
    }

    [HttpPost]
    [RequireRole(UserRole.Admin, UserRole.Reception)]
    public async Task<ActionResult<ClinicDto>> Create([FromBody] CreateClinicDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var created = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [RequireRole(UserRole.Admin, UserRole.Reception)]
    public async Task<ActionResult<ClinicDto>> Update(int id, [FromBody] UpdateClinicDto dto, CancellationToken ct)
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
