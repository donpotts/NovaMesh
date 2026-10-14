namespace NovaMesh.Data.Entities;

public class BackgroundJobRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string JobName { get; set; } = string.Empty;
    public string JobGroup { get; set; } = "Default";
    public string JobType { get; set; } = "DataSync";
    public string Description { get; set; } = string.Empty;
    public string CronSchedule { get; set; } = "0 * * * *";
    public string Status { get; set; } = "Active"; // Active, Running, Completed, Failed, Paused
    public DateTime? LastRunTime { get; set; }
    public DateTime? NextRunTime { get; set; }
    public long ExecutionDurationMs { get; set; } = 0;
    public int RunCount { get; set; } = 0;
    public string? ErrorMessage { get; set; }
    public string? PayloadJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}