using System.ComponentModel.DataAnnotations;

namespace DentalLab.Application.DTOs;

public class CaseItemDto
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ToothNumber { get; set; }
    public string? Material { get; set; }
    public string? Shade { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public string? Notes { get; set; }
}

public class CreateCaseItemDto
{
    [Required, StringLength(200)]
    public string ProductName { get; set; } = string.Empty;

    [StringLength(20)]
    public string? ToothNumber { get; set; }

    [StringLength(100)]
    public string? Material { get; set; }

    [StringLength(50)]
    public string? Shade { get; set; }

    [Range(1, 1000)]
    public int Quantity { get; set; } = 1;

    [Range(0, 9999999)]
    public decimal UnitPrice { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}
