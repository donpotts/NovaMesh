namespace NovaMesh.Data.Entities;

public class AppSetting
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Category { get; set; } = "General"; // General, Security, Email, Audit
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;
}