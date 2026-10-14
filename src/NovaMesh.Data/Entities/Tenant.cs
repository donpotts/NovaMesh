namespace NovaMesh.Data.Entities;

public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? ConnectionString { get; set; }
    public string EditionName { get; set; } = "Standard";
    public bool IsActive { get; set; } = true;
    public string PrimaryColor { get; set; } = "#4F46E5";
    public string SecondaryColor { get; set; } = "#06B6D4";
    public string AccentColor { get; set; } = "#F59E0B";
    public string? LogoUrl { get; set; }
    public string? CustomDomain { get; set; }
    public string? Slogan { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}