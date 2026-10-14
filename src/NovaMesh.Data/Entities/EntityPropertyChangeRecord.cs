namespace NovaMesh.Data.Entities;

public class EntityPropertyChangeRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EntityChangeId { get; set; }
    public EntityChangeRecord EntityChange { get; set; } = null!;
    public string PropertyName { get; set; } = string.Empty;
    public string? OriginalValue { get; set; }
    public string? NewValue { get; set; }
    public string PropertyType { get; set; } = "String";
}