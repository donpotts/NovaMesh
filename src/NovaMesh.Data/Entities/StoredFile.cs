namespace NovaMesh.Data.Entities;

public class StoredFile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long FileSizeBytes { get; set; }
    public string StorageProvider { get; set; } = "Local Disk";
    public string Category { get; set; } = "Documents";
    public string? Description { get; set; }
    public string? StoragePath { get; set; }
    public string UploadedBy { get; set; } = "admin@platform.local";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}