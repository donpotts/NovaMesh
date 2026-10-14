namespace NovaMesh.Data.Entities;

public class EntityChangeRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? AuditLogId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string ChangeType { get; set; } = "Updated"; // Created, Updated, Deleted
    public DateTime ChangeTime { get; set; } = DateTime.UtcNow;
    public string? UserName { get; set; }
    public string? TenantName { get; set; }
    public List<EntityPropertyChangeRecord> PropertyChanges { get; set; } = new();
}