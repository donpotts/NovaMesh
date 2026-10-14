namespace NovaMesh.Data.Entities;

public class AiDocumentChunk
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public AiDocument Document { get; set; } = null!;
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = string.Empty;
    public string EmbeddingVectorJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}