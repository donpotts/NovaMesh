namespace NovaMesh.Data.Entities;

public class AiAgentAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string AgentName { get; set; } = "Autonomous AI Agent";
    public string ActionName { get; set; } = "Tool Execution";
    public string InputPrompt { get; set; } = string.Empty;
    public string OutputSummary { get; set; } = string.Empty;
    public long ExecutionMs { get; set; }
    public bool Success { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}