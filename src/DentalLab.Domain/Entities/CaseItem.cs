namespace DentalLab.Domain.Entities;

public class CaseItem : BaseEntity
{
    public int CaseId { get; set; }
    public Case? Case { get; set; }

    public string ProductName { get; set; } = string.Empty;
    public string? ToothNumber { get; set; }
    public string? Material { get; set; }
    public string? Shade { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public string? Notes { get; set; }

    public decimal LineTotal => Quantity * UnitPrice;
}
