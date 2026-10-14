namespace NovaMesh.Data.Entities;

public class TextTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = "Email";
    public string Culture { get; set; } = "en-US";
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string AvailableTokens { get; set; } = string.Empty;
    public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;
}