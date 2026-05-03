using DentalLab.Application.Common;
using DentalLab.Application.DTOs;
using DentalLab.Application.Interfaces;
using DentalLab.Domain.Entities;
using DentalLab.Domain.Enums;
using DentalLab.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace DentalLab.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(IUnitOfWork uow, ILogger<PaymentService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto dto, string? performedBy, CancellationToken ct = default)
    {
        var caseEntity = await _uow.Cases.GetByIdAsync(dto.CaseId, ct)
            ?? throw new NotFoundException("Case", dto.CaseId);

        var subtotal = dto.Subtotal;
        if (subtotal <= 0)
        {
            var items = await _uow.CaseItems.ListAsync(i => i.CaseId == dto.CaseId, ct);
            subtotal = items.Sum(i => i.Quantity * i.UnitPrice);
            if (subtotal <= 0)
                subtotal = caseEntity.Price;
        }

        if (subtotal <= 0)
            throw new BusinessRuleException("Cannot create invoice with zero or negative amount.");

        var invoice = new Invoice
        {
            InvoiceNumber = await GenerateInvoiceNumberAsync(ct),
            CaseId = caseEntity.Id,
            ClinicId = caseEntity.ClinicId,
            IssueDate = DateTime.UtcNow,
            DueDate = dto.DueDate,
            Subtotal = subtotal,
            TaxAmount = dto.TaxAmount,
            TotalAmount = subtotal + dto.TaxAmount,
            AmountPaid = 0,
            Status = PaymentStatus.Pending,
            Notes = dto.Notes
        };

        await _uow.Invoices.AddAsync(invoice, ct);

        await _uow.ActivityLogs.AddAsync(new ActivityLog
        {
            Action = "CreateInvoice",
            EntityType = nameof(Invoice),
            EntityId = invoice.Id,
            Details = $"Invoice {invoice.InvoiceNumber} created for case {caseEntity.CaseNumber}, total {invoice.TotalAmount:0.00}",
            PerformedBy = performedBy
        }, ct);

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Invoice {InvoiceNumber} created for case {CaseId} total {Total} by {User}",
            invoice.InvoiceNumber, caseEntity.Id, invoice.TotalAmount, performedBy ?? "system");

        return await BuildInvoiceDtoAsync(invoice, ct);
    }

    public async Task<InvoiceDto?> GetInvoiceByIdAsync(int id, CancellationToken ct = default)
    {
        var invoice = await _uow.Invoices.GetByIdAsync(id, ct);
        return invoice == null ? null : await BuildInvoiceDtoAsync(invoice, ct);
    }

    public async Task<IReadOnlyList<InvoiceDto>> GetInvoicesByCaseAsync(int caseId, CancellationToken ct = default)
    {
        var invoices = await _uow.Invoices.ListAsync(i => i.CaseId == caseId, ct);
        var dtos = new List<InvoiceDto>();
        foreach (var inv in invoices)
            dtos.Add(await BuildInvoiceDtoAsync(inv, ct));
        return dtos;
    }

    public async Task<IReadOnlyList<InvoiceDto>> GetAllInvoicesAsync(CancellationToken ct = default)
    {
        var invoices = await _uow.Invoices.ListAllAsync(ct);
        var dtos = new List<InvoiceDto>();
        foreach (var inv in invoices.OrderByDescending(i => i.IssueDate))
            dtos.Add(await BuildInvoiceDtoAsync(inv, ct));
        return dtos;
    }

    public async Task<PaymentDto> RecordPaymentAsync(CreatePaymentDto dto, string? performedBy, CancellationToken ct = default)
    {
        var invoice = await _uow.Invoices.GetByIdAsync(dto.InvoiceId, ct)
            ?? throw new NotFoundException("Invoice", dto.InvoiceId);

        if (invoice.Status == PaymentStatus.Cancelled || invoice.Status == PaymentStatus.Refunded)
            throw new BusinessRuleException($"Cannot add payment to a {invoice.Status} invoice.");

        if (dto.Amount <= 0)
            throw new BusinessRuleException("Payment amount must be greater than zero.");

        var newTotalPaid = invoice.AmountPaid + dto.Amount;
        if (newTotalPaid > invoice.TotalAmount)
            throw new BusinessRuleException(
                $"Payment exceeds outstanding balance. Outstanding: {invoice.TotalAmount - invoice.AmountPaid:0.00}");

        var payment = new Payment
        {
            InvoiceId = invoice.Id,
            PaymentDate = DateTime.UtcNow,
            Amount = dto.Amount,
            Method = dto.Method,
            ReferenceNumber = dto.ReferenceNumber,
            Notes = dto.Notes,
            ReceivedBy = dto.ReceivedBy ?? performedBy
        };
        await _uow.Payments.AddAsync(payment, ct);

        invoice.AmountPaid = newTotalPaid;
        invoice.Status = newTotalPaid >= invoice.TotalAmount
            ? PaymentStatus.Paid
            : PaymentStatus.Partial;
        invoice.UpdatedAt = DateTime.UtcNow;
        _uow.Invoices.Update(invoice);

        await _uow.ActivityLogs.AddAsync(new ActivityLog
        {
            Action = "RecordPayment",
            EntityType = nameof(Payment),
            EntityId = payment.Id,
            Details = $"Payment of {payment.Amount:0.00} recorded for invoice {invoice.InvoiceNumber}.",
            PerformedBy = performedBy
        }, ct);

        await _uow.SaveChangesAsync(ct);

        return new PaymentDto
        {
            Id = payment.Id,
            InvoiceId = payment.InvoiceId,
            InvoiceNumber = invoice.InvoiceNumber,
            PaymentDate = payment.PaymentDate,
            Amount = payment.Amount,
            Method = payment.Method,
            ReferenceNumber = payment.ReferenceNumber,
            Notes = payment.Notes,
            ReceivedBy = payment.ReceivedBy
        };
    }

    public async Task<IReadOnlyList<PaymentDto>> GetPaymentsByInvoiceAsync(int invoiceId, CancellationToken ct = default)
    {
        var invoice = await _uow.Invoices.GetByIdAsync(invoiceId, ct)
            ?? throw new NotFoundException("Invoice", invoiceId);

        var payments = await _uow.Payments.ListAsync(p => p.InvoiceId == invoiceId, ct);
        return payments.OrderByDescending(p => p.PaymentDate)
            .Select(p => new PaymentDto
            {
                Id = p.Id,
                InvoiceId = p.InvoiceId,
                InvoiceNumber = invoice.InvoiceNumber,
                PaymentDate = p.PaymentDate,
                Amount = p.Amount,
                Method = p.Method,
                ReferenceNumber = p.ReferenceNumber,
                Notes = p.Notes,
                ReceivedBy = p.ReceivedBy
            }).ToList();
    }

    private async Task<string> GenerateInvoiceNumberAsync(CancellationToken ct)
    {
        var prefix = $"INV-{DateTime.UtcNow:yyyyMM}-";
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var monthEnd = monthStart.AddMonths(1);

        var monthCount = await _uow.Invoices.CountAsync(i =>
            i.IssueDate >= monthStart && i.IssueDate < monthEnd, ct);

        return prefix + (monthCount + 1).ToString("D5");
    }

    private async Task<InvoiceDto> BuildInvoiceDtoAsync(Invoice invoice, CancellationToken ct)
    {
        var caseEntity = await _uow.Cases.GetByIdAsync(invoice.CaseId, ct);
        var clinic = await _uow.Clinics.GetByIdAsync(invoice.ClinicId, ct);
        var payments = await _uow.Payments.ListAsync(p => p.InvoiceId == invoice.Id, ct);

        return new InvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            CaseId = invoice.CaseId,
            CaseNumber = caseEntity?.CaseNumber,
            ClinicId = invoice.ClinicId,
            ClinicName = clinic?.Name,
            IssueDate = invoice.IssueDate,
            DueDate = invoice.DueDate,
            Subtotal = invoice.Subtotal,
            TaxAmount = invoice.TaxAmount,
            TotalAmount = invoice.TotalAmount,
            AmountPaid = invoice.AmountPaid,
            AmountDue = invoice.AmountDue,
            Status = invoice.Status,
            Notes = invoice.Notes,
            Payments = payments.OrderByDescending(p => p.PaymentDate).Select(p => new PaymentDto
            {
                Id = p.Id,
                InvoiceId = p.InvoiceId,
                InvoiceNumber = invoice.InvoiceNumber,
                PaymentDate = p.PaymentDate,
                Amount = p.Amount,
                Method = p.Method,
                ReferenceNumber = p.ReferenceNumber,
                Notes = p.Notes,
                ReceivedBy = p.ReceivedBy
            }).ToList()
        };
    }
}
