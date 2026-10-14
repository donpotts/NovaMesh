namespace NovaMesh.Data.Entities;

public class AppNotification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info"; // Info, Success, Warning, Error
    public string TargetAudience { get; set; } = "All Users";
    public bool IsRead { get; set; } = false;
    public string? ActionUrl { get; set; }
    public string CreatedBy { get; set; } = "System";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}