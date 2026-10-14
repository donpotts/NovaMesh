namespace NovaMesh.Data.Entities;

public class LocalizationResourceText
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ResourceKey { get; set; } = string.Empty;
    public string CultureCode { get; set; } = "en-US";
    public string Value { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;
}