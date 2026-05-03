using System.ComponentModel.DataAnnotations;

namespace DentalLab.Application.DTOs;

public class WorkflowStageDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public int EstimatedDurationMinutes { get; set; }
}

public class CreateWorkflowStageDto
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(0, 1000)]
    public int Order { get; set; }

    [Range(0, 100000)]
    public int EstimatedDurationMinutes { get; set; }
}

public class UpdateWorkflowStageDto : CreateWorkflowStageDto
{
    public bool IsActive { get; set; } = true;
}

public class CaseStageHistoryDto
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public int StageId { get; set; }
    public string? StageName { get; set; }
    public int? TechnicianId { get; set; }
    public string? TechnicianName { get; set; }
    public DateTime EnteredAt { get; set; }
    public DateTime? ExitedAt { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Notes { get; set; }
}

public class MoveCaseToStageDto
{
    [Required]
    public int StageId { get; set; }

    public int? TechnicianId { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}
