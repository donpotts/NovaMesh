namespace NovaMesh.Data.Entities;

public class DistributedOrder
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string ServiceOrigin { get; set; } = "webfrontend";
    public string DestinationService { get; set; } = "apiservice";
    public string Status { get; set; } = "Completed";
    public decimal TotalAmount { get; set; }
    public long LatencyMs { get; set; }
    public string TraceId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}