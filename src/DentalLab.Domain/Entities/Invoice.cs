using DentalLab.Domain.Enums;

namespace DentalLab.Domain.Entities;

public class Invoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;

    public int CaseId { get; set; }
    public Case? Case { get; set; }

    public int ClinicId { get; set; }
    public Clinic? Clinic { get; set; }

    public DateTime IssueDate { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }

    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal AmountDue => TotalAmount - AmountPaid;

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? Notes { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
