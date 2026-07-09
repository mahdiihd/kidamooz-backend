namespace Kidamooz.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string EntityTitle { get; set; } = string.Empty;
    public string ActorEmail { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
