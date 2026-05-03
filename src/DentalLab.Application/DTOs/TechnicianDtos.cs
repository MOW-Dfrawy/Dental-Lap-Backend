using System.ComponentModel.DataAnnotations;

namespace DentalLab.Application.DTOs;

public class TechnicianDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Specialty { get; set; }
    public bool IsActive { get; set; }
}

public class CreateTechnicianDto
{
    [Required, StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress, StringLength(150)]
    public string? Email { get; set; }

    [StringLength(50)]
    public string? Phone { get; set; }

    [StringLength(100)]
    public string? Specialty { get; set; }
}

public class UpdateTechnicianDto : CreateTechnicianDto
{
    public bool IsActive { get; set; } = true;
}
