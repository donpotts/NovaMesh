using Microsoft.EntityFrameworkCore;
using NovaMesh.Data.Entities;

namespace NovaMesh.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Core Identity Module
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<AppRole> Roles => Set<AppRole>();
    public DbSet<AppUserRole> UserRoles => Set<AppUserRole>();
    public DbSet<PermissionGrant> PermissionGrants => Set<PermissionGrant>();

    // Core Audit Logging & Settings Modules
    public DbSet<AuditLogEntry> AuditLogs => Set<AuditLogEntry>();
    public DbSet<AppSetting> Settings => Set<AppSetting>();

    // SaaS Multi-Tenancy & Features Modules
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<FeatureValue> FeatureValues => Set<FeatureValue>();

    // AI Document Knowledge Base, RAG & Agents Module
    public DbSet<AiDocument> AiDocuments => Set<AiDocument>();
    public DbSet<AiDocumentChunk> AiChunks => Set<AiDocumentChunk>();
    public DbSet<AiAgentConversation> AiConversations => Set<AiAgentConversation>();
    public DbSet<AiAgentMessage> AiMessages => Set<AiAgentMessage>();
    public DbSet<AiAgentAuditLog> AiAgentAuditLogs => Set<AiAgentAuditLog>();

    // Enterprise Background Jobs & Entity Property Audit Trail
    public DbSet<BackgroundJobRecord> BackgroundJobs => Set<BackgroundJobRecord>();
    public DbSet<EntityChangeRecord> EntityChanges => Set<EntityChangeRecord>();
    public DbSet<EntityPropertyChangeRecord> EntityPropertyChanges => Set<EntityPropertyChangeRecord>();

    // Enterprise File Management, Notifications, Templates, Localization & OAuth
    public DbSet<StoredFile> Files => Set<StoredFile>();
    public DbSet<AppNotification> Notifications => Set<AppNotification>();
    public DbSet<TextTemplate> TextTemplates => Set<TextTemplate>();
    public DbSet<LanguageRecord> Languages => Set<LanguageRecord>();
    public DbSet<LocalizationResourceText> LocalizationTexts => Set<LocalizationResourceText>();
    public DbSet<OAuthClientApp> OAuthClients => Set<OAuthClientApp>();

    // CMS Kit Blog Hub (Common across all architectures)
    public DbSet<Blog> Blogs => Set<Blog>();
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
    public DbSet<BlogPostReaction> Reactions => Set<BlogPostReaction>();
    public DbSet<CmsComment> Comments => Set<CmsComment>();

    // Domain Specific Module
    public DbSet<CloudResource> CloudResources => Set<CloudResource>();
    public DbSet<DistributedOrder> DistributedOrders => Set<DistributedOrder>();
    public DbSet<DistributedTraceRecord> TraceRecords => Set<DistributedTraceRecord>();

    // Dynamic Injected Entity: Invoice
    public DbSet<Invoice> Invoices => Set<Invoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        modelBuilder.Entity<AppUserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId);

        modelBuilder.Entity<AppUserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId);

        modelBuilder.Entity<AiDocumentChunk>()
            .HasOne(c => c.Document)
            .WithMany(d => d.Chunks)
            .HasForeignKey(c => c.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AiAgentMessage>()
            .HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EntityPropertyChangeRecord>()
            .HasOne(p => p.EntityChange)
            .WithMany(c => c.PropertyChanges)
            .HasForeignKey(p => p.EntityChangeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}