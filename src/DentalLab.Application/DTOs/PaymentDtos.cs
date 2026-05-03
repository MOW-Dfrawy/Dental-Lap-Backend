using System.ComponentModel.DataAnnotations;

namespace DentalLab.Application.DTOs;

public class PaymentDto
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public string? ReceivedBy { get; set; }
}

public class CreatePaymentDto
{
    [Required]
    public int InvoiceId { get; set; }

    [Range(0.01, 9999999)]
    public decimal Amount { get; set; }

    [Required, StringLength(50)]
    public string Method { get; set; } = "Cash";

    [StringLength(100)]
    public string? ReferenceNumber { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    [StringLength(150)]
    public string? ReceivedBy { get; set; }
}
