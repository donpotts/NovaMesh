namespace NovaMesh.Data.Entities;

public class AiAgentMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public AiAgentConversation Conversation { get; set; } = null!;
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    public string CitationsJson { get; set; } = "[]";
    public string ToolCallsJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}