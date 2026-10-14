namespace NovaMesh.Data.Entities;

public class AppRole
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PermissionsJson { get; set; } = "[]";
    public List<AppUserRole> UserRoles { get; set; } = new();
}