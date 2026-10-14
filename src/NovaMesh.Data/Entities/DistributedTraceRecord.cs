namespace NovaMesh.Data.Entities;

public class DistributedTraceRecord
{
    public int Id { get; set; }
    public string TraceId { get; set; } = string.Empty;
    public string SpanId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string OperationName { get; set; } = string.Empty;
    public long DurationMs { get; set; }
    public int StatusCode { get; set; } = 200;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}