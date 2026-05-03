using DentalLab.Domain.Enums;

namespace DentalLab.Domain.Entities;

public class Delivery : BaseEntity
{
    public int CaseId { get; set; }
    public Case? Case { get; set; }

    public DateTime? ScheduledDate { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    public string? RecipientName { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? Courier { get; set; }
    public string? TrackingNumber { get; set; }

    public DeliveryStatus Status { get; set; } = DeliveryStatus.Pending;
    public string? Notes { get; set; }
}
