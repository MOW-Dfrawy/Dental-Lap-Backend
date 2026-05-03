namespace DentalLab.Domain.Entities;

public class CaseStageHistory : BaseEntity
{
    public int CaseId { get; set; }
    public Case? Case { get; set; }

    public int StageId { get; set; }
    public WorkflowStage? Stage { get; set; }

    public int? TechnicianId { get; set; }
    public Technician? Technician { get; set; }

    public DateTime EnteredAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExitedAt { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Notes { get; set; }
}
