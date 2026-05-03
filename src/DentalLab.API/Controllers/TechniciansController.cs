using DentalLab.API.Middleware;
using DentalLab.Application.DTOs;
using DentalLab.Application.Interfaces;
using DentalLab.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace DentalLab.API.Controllers;

[ApiController]
[Route("api/technicians")]
public class TechniciansController : ControllerBase
{
    private readonly ITechnicianService _service;

    public TechniciansController(ITechnicianService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequireRole(UserRole.Admin, UserRole.Reception, UserRole.Technician)]
    public async Task<ActionResult<IReadOnlyList<TechnicianDto>>> Get(CancellationToken ct)
        => Ok(await _service.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    [RequireRole(UserRole.Admin, UserRole.Reception, UserRole.Technician)]
    public async Task<ActionResult<TechnicianDto>> GetById(int id, CancellationToken ct)
    {
        var t = await _service.GetByIdAsync(id, ct);
        return t == null ? NotFound() : Ok(t);
    }

    [HttpPost]
    [RequireRole(UserRole.Admin)]
    public async Task<ActionResult<TechnicianDto>> Create([FromBody] CreateTechnicianDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var created = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [RequireRole(UserRole.Admin)]
    public async Task<ActionResult<TechnicianDto>> Update(int id, [FromBody] UpdateTechnicianDto dto, CancellationToken ct)
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
