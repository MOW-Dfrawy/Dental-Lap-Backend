namespace DentalLab.Domain.Entities;

public class Payment : BaseEntity
{
    public int InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public decimal Amount { get; set; }
    public string Method { get; set; } = "Cash"; // Cash, Card, Bank Transfer, Cheque
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public string? ReceivedBy { get; set; }
}
