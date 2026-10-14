namespace NovaMesh.Data.Entities;

public class CmsComment
{
    public int Id { get; set; }
    public int ArticleId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorEmail { get; set; } = string.Empty;
    public string CommentText { get; set; } = string.Empty;
    public string Status { get; set; } = "Approved"; // Approved, Pending, Spam
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
