namespace NovaMesh.Data.Entities;

public class AiDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SourceType { get; set; } = "Documentation";
    public string Content { get; set; } = string.Empty;
    public int ChunkCount { get; set; }
    public int TokenCount { get; set; }
    public string EmbeddingModel { get; set; } = "nomic-embed-text:latest";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<AiDocumentChunk> Chunks { get; set; } = new();
}