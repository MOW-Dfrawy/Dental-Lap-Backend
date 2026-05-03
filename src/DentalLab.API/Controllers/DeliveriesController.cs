using DentalLab.API.Middleware;
using DentalLab.Application.DTOs;
using DentalLab.Application.Interfaces;
using DentalLab.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace DentalLab.API.Controllers;

[ApiController]
[Route("api/deliveries")]
public class DeliveriesController : ControllerBase
{
    private readonly IDeliveryService _service;

    public DeliveriesController(IDeliveryService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequireRole(UserRole.Admin, UserRole.Reception, UserRole.Technician)]
    public async Task<ActionResult<IReadOnlyList<DeliveryDto>>> Get(CancellationToken ct)
        => Ok(await _service.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    [RequireRole(UserRole.Admin, UserRole.Reception, UserRole.Technician)]
    public async Task<ActionResult<DeliveryDto>> GetById(int id, CancellationToken ct)
    {
        var d = await _service.GetByIdAsync(id, ct);
        return d == null ? NotFound() : Ok(d);
    }

    [HttpGet("by-case/{caseId:int}")]
    [RequireRole(UserRole.Admin, UserRole.Reception, UserRole.Technician)]
    public async Task<ActionResult<IReadOnlyList<DeliveryDto>>> GetByCase(int caseId, CancellationToken ct)
        => Ok(await _service.GetByCaseAsync(caseId, ct));

    [HttpPost]
    [RequireRole(UserRole.Admin, UserRole.Reception)]
    public async Task<ActionResult<DeliveryDto>> Create([FromBody] CreateDeliveryDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var user = HttpContext.GetUserName();
        var created = await _service.CreateAsync(dto, user, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}/status")]
    [RequireRole(UserRole.Admin, UserRole.Reception)]
    public async Task<ActionResult<DeliveryDto>> UpdateStatus(int id, [FromBody] UpdateDeliveryStatusDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var user = HttpContext.GetUserName();
        return Ok(await _service.UpdateStatusAsync(id, dto, user, ct));
    }
}
