using DentalLab.API.Middleware;
using DentalLab.Application.DTOs;
using DentalLab.Application.Interfaces;
using DentalLab.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace DentalLab.API.Controllers;

[ApiController]
[Route("api/invoices")]
public class InvoicesController : ControllerBase
{
    private readonly IPaymentService _service;

    public InvoicesController(IPaymentService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequireRole(UserRole.Admin, UserRole.Reception)]
    public async Task<ActionResult<IReadOnlyList<InvoiceDto>>> Get(CancellationToken ct)
        => Ok(await _service.GetAllInvoicesAsync(ct));

    [HttpGet("{id:int}")]
    [RequireRole(UserRole.Admin, UserRole.Reception)]
    public async Task<ActionResult<InvoiceDto>> GetById(int id, CancellationToken ct)
    {
        var inv = await _service.GetInvoiceByIdAsync(id, ct);
        return inv == null ? NotFound() : Ok(inv);
    }

    [HttpGet("by-case/{caseId:int}")]
    [RequireRole(UserRole.Admin, UserRole.Reception)]
    public async Task<ActionResult<IReadOnlyList<InvoiceDto>>> GetByCase(int caseId, CancellationToken ct)
        => Ok(await _service.GetInvoicesByCaseAsync(caseId, ct));

    [HttpPost]
    [RequireRole(UserRole.Admin, UserRole.Reception)]
    public async Task<ActionResult<InvoiceDto>> Create([FromBody] CreateInvoiceDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var user = HttpContext.GetUserName();
        var created = await _service.CreateInvoiceAsync(dto, user, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
