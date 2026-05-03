using System.ComponentModel.DataAnnotations;

namespace DentalLab.Application.DTOs;

public class PatientDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }
    public int ClinicId { get; set; }
    public string? ClinicName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreatePatientDto
{
    [Required, StringLength(200)]
    public string FullName { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }

    [StringLength(20)]
    public string? Gender { get; set; }

    [StringLength(50)]
    public string? Phone { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    [Required]
    public int ClinicId { get; set; }
}

public class UpdatePatientDto : CreatePatientDto { }
