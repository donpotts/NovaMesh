namespace NovaMesh.Data.Entities;

public class AiAgentConversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "Enterprise Assistant Session";
    public string Persona { get; set; } = "Solution Architect";
    public string Model { get; set; } = "gemma4:31b";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<AiAgentMessage> Messages { get; set; } = new();
}