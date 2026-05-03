using DentalLab.API.Middleware;
using DentalLab.Application.DTOs;
using DentalLab.Application.Interfaces;
using DentalLab.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace DentalLab.API.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _service;

    public PaymentsController(IPaymentService service)
    {
        _service = service;
    }

    [HttpPost]
    [RequireRole(UserRole.Admin, UserRole.Reception)]
    public async Task<ActionResult<PaymentDto>> Record([FromBody] CreatePaymentDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var user = HttpContext.GetUserName();
        var payment = await _service.RecordPaymentAsync(dto, user, ct);
        return Ok(payment);
    }

    [HttpGet("by-invoice/{invoiceId:int}")]
    [RequireRole(UserRole.Admin, UserRole.Reception)]
    public async Task<ActionResult<IReadOnlyList<PaymentDto>>> GetByInvoice(int invoiceId, CancellationToken ct)
        => Ok(await _service.GetPaymentsByInvoiceAsync(invoiceId, ct));
}
