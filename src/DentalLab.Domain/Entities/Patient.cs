namespace DentalLab.Domain.Entities;

public class Patient : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }

    public int ClinicId { get; set; }
    public Clinic? Clinic { get; set; }

    public ICollection<Case> Cases { get; set; } = new List<Case>();
}
