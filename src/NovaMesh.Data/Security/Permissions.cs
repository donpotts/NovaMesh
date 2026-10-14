namespace NovaMesh.Data.Security;

public static class Permissions
{
    // Identity & RBAC
    public const string UsersView = "Identity.Users.View";
    public const string UsersCreate = "Identity.Users.Create";
    public const string UsersUpdate = "Identity.Users.Update";
    public const string UsersDelete = "Identity.Users.Delete";
    public const string RolesView = "Identity.Roles.View";
    public const string RolesManage = "Identity.Roles.Manage";

    // Audit Logging
    public const string AuditLogsView = "AuditLogging.View";
    public const string AuditLogsManage = "AuditLogging.Manage";

    // System Settings
    public const string SettingsView = "Settings.View";
    public const string SettingsManage = "Settings.Manage";

    // Domain Specific
    public const string DomainRead = "Domain.Read";
    public const string DomainCreate = "Domain.Create";
    public const string DomainManage = "Domain.Manage";

    public static readonly string[] All = [
        UsersView, UsersCreate, UsersUpdate, UsersDelete, 
        RolesView, RolesManage,
        AuditLogsView, AuditLogsManage,
        SettingsView, SettingsManage,
        DomainRead, DomainCreate, DomainManage
    ];

    public static readonly string[] Staff = [
        UsersView, RolesView, AuditLogsView, SettingsView, DomainRead, DomainCreate, DomainManage
    ];

    public static readonly string[] Standard = [DomainRead];
}