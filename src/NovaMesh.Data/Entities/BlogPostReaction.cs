namespace NovaMesh.Data.Entities;

public class BlogPostReaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BlogPostId { get; set; }
    public string ReactionName { get; set; } = "Like";
    public string UserName { get; set; } = "anonymous";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
