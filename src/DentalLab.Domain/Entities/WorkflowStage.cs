namespace DentalLab.Domain.Entities;

public class WorkflowStage : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
    public int EstimatedDurationMinutes { get; set; }
}
