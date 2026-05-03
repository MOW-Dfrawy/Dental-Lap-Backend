using System.ComponentModel.DataAnnotations;
using DentalLab.Domain.Enums;

namespace DentalLab.Application.DTOs;

public class InvoiceDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int CaseId { get; set; }
    public string? CaseNumber { get; set; }
    public int ClinicId { get; set; }
    public string? ClinicName { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal AmountDue { get; set; }
    public PaymentStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string? Notes { get; set; }
    public List<PaymentDto> Payments { get; set; } = new();
}

public class CreateInvoiceDto
{
    [Required]
    public int CaseId { get; set; }

    public DateTime? DueDate { get; set; }

    [Range(0, 9999999)]
    public decimal Subtotal { get; set; }

    [Range(0, 9999999)]
    public decimal TaxAmount { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}
