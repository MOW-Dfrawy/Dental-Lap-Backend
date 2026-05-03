namespace DentalLab.Domain.Entities;

public class Technician : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Specialty { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Case> Cases { get; set; } = new List<Case>();
}
