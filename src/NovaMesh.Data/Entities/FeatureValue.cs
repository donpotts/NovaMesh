namespace NovaMesh.Data.Entities;

public class FeatureValue
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = "true";
    public string ProviderName { get; set; } = "Global";
    public string? ProviderKey { get; set; }
    public string Description { get; set; } = string.Empty;
}