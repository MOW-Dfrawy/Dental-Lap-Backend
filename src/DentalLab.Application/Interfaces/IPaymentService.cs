using DentalLab.Application.DTOs;

namespace DentalLab.Application.Interfaces;

public interface IPaymentService
{
    Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto dto, string? performedBy, CancellationToken ct = default);
    Task<InvoiceDto?> GetInvoiceByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<InvoiceDto>> GetInvoicesByCaseAsync(int caseId, CancellationToken ct = default);
    Task<IReadOnlyList<InvoiceDto>> GetAllInvoicesAsync(CancellationToken ct = default);

    Task<PaymentDto> RecordPaymentAsync(CreatePaymentDto dto, string? performedBy, CancellationToken ct = default);
    Task<IReadOnlyList<PaymentDto>> GetPaymentsByInvoiceAsync(int invoiceId, CancellationToken ct = default);
}
