namespace NovaMesh.Data.Entities;

public class PermissionGrant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RoleName { get; set; } = string.Empty;
    public string PermissionKey { get; set; } = string.Empty;
    public bool IsGranted { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}