namespace DentalLab.Domain.Entities;

public class ActivityLog : BaseEntity
{
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string? Details { get; set; }
    public string? PerformedBy { get; set; }
    public string? Role { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
