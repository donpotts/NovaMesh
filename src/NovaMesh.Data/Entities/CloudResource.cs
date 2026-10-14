namespace NovaMesh.Data.Entities;

public class CloudResource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ResourceType { get; set; } = "Project";
    public string EndpointUrl { get; set; } = string.Empty;
    public string State { get; set; } = "Healthy";
    public int ReplicaCount { get; set; } = 1;
    public int MemoryMb { get; set; } = 128;
    public double CpuPercentage { get; set; } = 1.2;
    public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;
}