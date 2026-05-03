using DentalLab.Domain.Enums;

namespace DentalLab.Domain.Entities;

public class Case : BaseEntity
{
    public string CaseNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public int ClinicId { get; set; }
    public Clinic? Clinic { get; set; }

    public int PatientId { get; set; }
    public Patient? Patient { get; set; }

    public int? AssignedTechnicianId { get; set; }
    public Technician? AssignedTechnician { get; set; }

    public int? CurrentStageId { get; set; }
    public WorkflowStage? CurrentStage { get; set; }

    public CaseStatus Status { get; set; } = CaseStatus.New;

    public DateTime ReceivedDate { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public decimal Price { get; set; }
    public string? Priority { get; set; }
    public string? ToothShade { get; set; }
    public string? Notes { get; set; }

    public ICollection<CaseItem> Items { get; set; } = new List<CaseItem>();
    public ICollection<CaseStageHistory> StageHistory { get; set; } = new List<CaseStageHistory>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();
}
