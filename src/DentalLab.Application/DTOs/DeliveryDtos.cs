using System.ComponentModel.DataAnnotations;
using DentalLab.Domain.Enums;

namespace DentalLab.Application.DTOs;

public class DeliveryDto
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public string? CaseNumber { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? RecipientName { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? Courier { get; set; }
    public string? TrackingNumber { get; set; }
    public DeliveryStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string? Notes { get; set; }
}

public class CreateDeliveryDto
{
    [Required]
    public int CaseId { get; set; }

    public DateTime? ScheduledDate { get; set; }

    [StringLength(150)]
    public string? RecipientName { get; set; }

    [StringLength(300)]
    public string? DeliveryAddress { get; set; }

    [StringLength(100)]
    public string? Courier { get; set; }

    [StringLength(100)]
    public string? TrackingNumber { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}

public class UpdateDeliveryStatusDto
{
    [Required]
    public DeliveryStatus Status { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}
