namespace NovaMesh.Data.Entities;

public class AuditLogEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime ExecutionTime { get; set; } = DateTime.UtcNow;
    public string? UserName { get; set; }
    public string HttpMethod { get; set; } = "GET";
    public string Url { get; set; } = string.Empty;
    public int HttpStatusCode { get; set; } = 200;
    public long DurationMs { get; set; }
    public string? ClientIpAddress { get; set; }
    public string? BrowserInfo { get; set; }
    public string? ActionName { get; set; }
    public string? Exception { get; set; }
}