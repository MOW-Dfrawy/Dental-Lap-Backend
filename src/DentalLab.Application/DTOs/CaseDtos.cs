using System.ComponentModel.DataAnnotations;
using DentalLab.Domain.Enums;

namespace DentalLab.Application.DTOs;

public class CaseDto
{
    public int Id { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public int ClinicId { get; set; }
    public string? ClinicName { get; set; }
    public int PatientId { get; set; }
    public string? PatientName { get; set; }
    public int? AssignedTechnicianId { get; set; }
    public string? AssignedTechnicianName { get; set; }

    public int? CurrentStageId { get; set; }
    public string? CurrentStageName { get; set; }

    public CaseStatus Status { get; set; }
    public string StatusName => Status.ToString();

    public DateTime ReceivedDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? TotalDurationMinutes { get; set; }

    public decimal Price { get; set; }
    public string? Priority { get; set; }
    public string? ToothShade { get; set; }
    public string? Notes { get; set; }

    public List<CaseItemDto> Items { get; set; } = new();
    public List<CaseStageHistoryDto> StageHistory { get; set; } = new();
}

public class CreateCaseDto
{
    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required]
    public int ClinicId { get; set; }

    [Required]
    public int PatientId { get; set; }

    public int? AssignedTechnicianId { get; set; }
    public DateTime? DueDate { get; set; }

    [Range(0, 9999999)]
    public decimal Price { get; set; }

    [StringLength(20)]
    public string? Priority { get; set; }

    [StringLength(50)]
    public string? ToothShade { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    public List<CreateCaseItemDto> Items { get; set; } = new();
}

public class UpdateCaseDto
{
    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public int? AssignedTechnicianId { get; set; }
    public DateTime? DueDate { get; set; }
    public CaseStatus Status { get; set; }

    [Range(0, 9999999)]
    public decimal Price { get; set; }

    [StringLength(20)]
    public string? Priority { get; set; }

    [StringLength(50)]
    public string? ToothShade { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}

public class CaseFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public CaseStatus? Status { get; set; }
    public int? ClinicId { get; set; }
    public int? TechnicianId { get; set; }
    public int? PatientId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = true;
}
