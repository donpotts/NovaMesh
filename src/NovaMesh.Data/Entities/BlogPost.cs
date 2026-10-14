namespace NovaMesh.Data.Entities;

public class BlogPost
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BlogId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string CoverImageUrl { get; set; } = string.Empty;
    public string AuthorName { get; set; } = "Admin";
    public string AuthorAvatarUrl { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string Tags { get; set; } = string.Empty;
    public int ReadMinutes { get; set; } = 5;
    public int ViewCount { get; set; }
    public int LikeCount { get; set; }
    public bool IsPublished { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; } = DateTime.UtcNow;
}
