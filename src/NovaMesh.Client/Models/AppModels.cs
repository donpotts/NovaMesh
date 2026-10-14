namespace NovaMesh.Client.Models;

public record LoginRequest { public string UserName { get; set; } = ""; public string Password { get; set; } = ""; }
public record RegisterRequest { public string UserName { get; set; } = ""; public string Email { get; set; } = ""; public string Password { get; set; } = ""; public string FullName { get; set; } = ""; }
public record LoginResponse(string Token, string UserName, string Email, string FullName, List<string> Roles, List<string> Permissions);
public record UserDto(Guid Id, string UserName, string Email, string FullName, bool IsActive, DateTime CreatedAt, List<string> Roles);
public record CreateUserRequest(string UserName, string Email, string FullName, string Password, List<string> Roles);
public record RoleDto(Guid Id, string Name, string Description, List<string> Permissions);
public record CreateRoleRequest(string Name, string Description, List<string> Permissions);
public record UpdateRolePermissionsRequest(List<string> Permissions);
public record PermissionGroupDto(string GroupName, List<string> Permissions);
public record UserProfileDto(Guid Id, string UserName, string Email, string FullName, string? AvatarUrl, string? PhoneNumber, DateTime CreatedAt, List<string> Roles, List<string> Permissions);
public record UpdateProfileRequest(string FullName, string Email, string? PhoneNumber);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record PagedResultDto<T>(List<T> Items, int TotalCount, int PageNumber, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize));
}

public record AuditLogDto(Guid Id, DateTime ExecutionTime, string? UserName, string HttpMethod, string Url, int HttpStatusCode, long DurationMs, string? ClientIpAddress, string? BrowserInfo, string? ActionName);
public class AppSettingDto
{
    public Guid Id { get; set; }
    public string Category { get; set; } = "";
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime LastModifiedAt { get; set; }
}
public record UpdateSettingRequest(string Key, string Value);

// SaaS Multi-Tenancy Models
public class TenantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string NormalizedName { get; set; } = "";
    public string? ConnectionString { get; set; }
    public string EditionName { get; set; } = "Standard";
    public bool IsActive { get; set; } = true;
    public string PrimaryColor { get; set; } = "#4F46E5";
    public string SecondaryColor { get; set; } = "#06B6D4";
    public string AccentColor { get; set; } = "#F59E0B";
    public string? LogoUrl { get; set; }
    public string? CustomDomain { get; set; }
    public string? Slogan { get; set; }
    public DateTime CreatedAt { get; set; }
}
public record CreateTenantRequest(string Name, string EditionName, string? ConnectionString, string? PrimaryColor = null, string? SecondaryColor = null, string? AccentColor = null, string? LogoUrl = null, string? CustomDomain = null, string? Slogan = null);
public record UpdateTenantRequest(string Name, string EditionName, bool IsActive, string? ConnectionString, string? PrimaryColor = null, string? SecondaryColor = null, string? AccentColor = null, string? LogoUrl = null, string? CustomDomain = null, string? Slogan = null);

// Feature Management Models
public class FeatureValueDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Value { get; set; } = "true";
    public string ProviderName { get; set; } = "Global";
    public string? ProviderKey { get; set; }
    public string Description { get; set; } = "";
}
public record UpdateFeatureRequest(string Value);

// CMS Models
public class CmsArticle { public int Id { get; set; } public string Title { get; set; } = ""; public string Slug { get; set; } = ""; public string Summary { get; set; } = ""; public string Content { get; set; } = ""; public string Category { get; set; } = "General"; public string Author { get; set; } = "admin"; public bool IsPublished { get; set; } public int ViewCount { get; set; } public int ReadMinutes { get; set; } = 4; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public DateTime? PublishedAt { get; set; } }
public class CmsCategory { public int Id { get; set; } public string Name { get; set; } = ""; public string Slug { get; set; } = ""; public string Description { get; set; } = ""; public int DisplayOrder { get; set; } }
public class CmsComment { public int Id { get; set; } public int ArticleId { get; set; } public string AuthorName { get; set; } = ""; public string AuthorEmail { get; set; } = ""; public string CommentText { get; set; } = ""; public string Status { get; set; } = "Approved"; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class TodoItem { public int Id { get; set; } public string Title { get; set; } = ""; public bool IsCompleted { get; set; } }

// CMS Kit Blog Models
public class BlogDto { public Guid Id { get; set; } public string Name { get; set; } = ""; public string Slug { get; set; } = ""; public string Description { get; set; } = ""; public DateTime CreatedAt { get; set; } }
public class BlogPostDto
{
    public Guid Id { get; set; }
    public Guid BlogId { get; set; }
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string ShortDescription { get; set; } = "";
    public string Content { get; set; } = "";
    public string CoverImageUrl { get; set; } = "";
    public string AuthorName { get; set; } = "Admin";
    public string AuthorAvatarUrl { get; set; } = "";
    public string Category { get; set; } = "General";
    public string Tags { get; set; } = "";
    public int ReadMinutes { get; set; } = 5;
    public int ViewCount { get; set; }
    public int LikeCount { get; set; }
    public bool IsPublished { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
}
public record ReactRequest(string ReactionName);
public record PostCommentRequest(string AuthorName, string AuthorEmail, string CommentText);

// Store Models
public class Product { public int Id { get; set; } public string Title { get; set; } = ""; public string Sku { get; set; } = ""; public decimal Price { get; set; } public string Category { get; set; } = "General"; public int StockQuantity { get; set; } public double Rating { get; set; } = 4.8; public string Description { get; set; } = ""; }
public class ProductCategory { public int Id { get; set; } public string Name { get; set; } = ""; public string Slug { get; set; } = ""; public string Icon { get; set; } = "fas fa-box"; }
public class Order { public int Id { get; set; } public string OrderNumber { get; set; } = ""; public string CustomerName { get; set; } = ""; public string CustomerEmail { get; set; } = ""; public decimal TotalAmount { get; set; } public string Status { get; set; } = "Pending"; public int ItemCount { get; set; } = 1; public string ShippingAddress { get; set; } = ""; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class CustomerReview { public int Id { get; set; } public int ProductId { get; set; } public string CustomerName { get; set; } = ""; public int Rating { get; set; } = 5; public string Title { get; set; } = ""; public string ReviewText { get; set; } = ""; }

// CRM Models
public class Deal { public int Id { get; set; } public string DealName { get; set; } = ""; public string Company { get; set; } = ""; public decimal Value { get; set; } public string Stage { get; set; } = "Lead"; public string Owner { get; set; } = "admin"; public int Probability { get; set; } = 50; public DateTime ExpectedCloseDate { get; set; } = DateTime.UtcNow.AddMonths(1); }
public class Contact { public int Id { get; set; } public string FullName { get; set; } = ""; public string Company { get; set; } = ""; public string Email { get; set; } = ""; public string Phone { get; set; } = ""; public string Title { get; set; } = ""; }
public class Company { public int Id { get; set; } public string Name { get; set; } = ""; public string Industry { get; set; } = "Technology"; public decimal AnnualRevenue { get; set; } public string Website { get; set; } = ""; public string Phone { get; set; } = ""; public string City { get; set; } = ""; }
public class DealActivity { public int Id { get; set; } public int DealId { get; set; } public string ActivityType { get; set; } = "Meeting"; public string Summary { get; set; } = ""; public string Details { get; set; } = ""; public string CreatedBy { get; set; } = "admin"; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }

// AI Document Knowledge Base & RAG Models
public class AiDocumentDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
    public string SourceType { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Content { get; set; } = "";
    public int ChunkCount { get; set; }
    public int Tokens { get; set; }
    public string EmbeddingModel { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public record CreateAiDocumentRequest(string Title, string Category, string SourceType, string Content, string? Summary = null);

public class AiChunkDto
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = "";
    public int TokenCount { get; set; }
    public string MetadataJson { get; set; } = "";
    public double Similarity { get; set; }
}

public record RagCitationDto(string DocumentTitle, int ChunkIndex, double Similarity, string TextSnippet);
public record RagQueryRequest(string Query, double SimilarityThreshold = 0.70, int TopK = 4);
public record RagQueryResponse(string Query, string Answer, List<RagCitationDto> Citations, long ExecutionDurationMs, string GroundedPromptSnippet);

// Autonomous AI Agent Models
public class AiConversationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Persona { get; set; } = "";
    public string Model { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public int MessageCount { get; set; }
}

public record CreateConversationRequest(string Title, string Persona, string Model = "gemma4:31b");
public record AiToolCallDto(string ToolName, string Arguments, long DurationMs, string Result);

public class AiMessageDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public string Role { get; set; } = "";
    public string Content { get; set; } = "";
    public List<RagCitationDto> Citations { get; set; } = new();
    public List<AiToolCallDto> ToolCalls { get; set; } = new();
    public DateTime CreatedAt { get; set; }

    public AiMessageDto() { }
    public AiMessageDto(Guid id, Guid conversationId, string role, string content, List<RagCitationDto> citations, List<AiToolCallDto> toolCalls, DateTime createdAt)
    {
        Id = id;
        ConversationId = conversationId;
        Role = role;
        Content = content;
        Citations = citations;
        ToolCalls = toolCalls;
        CreatedAt = createdAt;
    }
}

public record SendAgentMessageRequest(string UserPrompt, string Persona = "Solution Architect", bool EnableRag = true, bool EnableAutonomousTools = true, string? Model = null);

public class AiAgentAuditLogDto
{
    public Guid Id { get; set; }
    public string AgentName { get; set; } = "";
    public string ActionName { get; set; } = "";
    public string InputPrompt { get; set; } = "";
    public string OutputSummary { get; set; } = "";
    public long ExecutionMs { get; set; }
    public bool Success { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Enterprise Dashboard & Analytics Telemetry Models
public class DashboardAnalyticsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalAuditLogs { get; set; }
    public long AvgLatencyMs { get; set; }
    public double SuccessRatePercent { get; set; }
    public int TotalAiDocs { get; set; }
    public int TotalRagChunks { get; set; }
    public int TotalAgentExecutions { get; set; }
    public string[] HourlyTrafficLabels { get; set; } = Array.Empty<string>();
    public double[] RequestCounts { get; set; } = Array.Empty<double>();
    public double[] LatencyMs { get; set; } = Array.Empty<double>();
    public string[] StatusCodeLabels { get; set; } = Array.Empty<string>();
    public double[] StatusCodeCounts { get; set; } = Array.Empty<double>();
    public string[] AiUsageLabels { get; set; } = Array.Empty<string>();
    public double[] AiTokensConsumed { get; set; } = Array.Empty<double>();
    public double[] AiVectorQueries { get; set; } = Array.Empty<double>();
    public string[] TenantDistributionLabels { get; set; } = Array.Empty<string>();
    public double[] TenantCounts { get; set; } = Array.Empty<double>();
}

// Background Jobs Queue Models
public class BackgroundJobDto
{
    public Guid Id { get; set; }
    public string JobName { get; set; } = "";
    public string JobGroup { get; set; } = "Default";
    public string JobType { get; set; } = "DataSync";
    public string Description { get; set; } = "";
    public string CronSchedule { get; set; } = "0 * * * *";
    public string Status { get; set; } = "Active";
    public DateTime? LastRunTime { get; set; }
    public DateTime? NextRunTime { get; set; }
    public long ExecutionDurationMs { get; set; }
    public int RunCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string? PayloadJson { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateBackgroundJobRequest
{
    public string JobName { get; set; } = "";
    public string JobGroup { get; set; } = "Default";
    public string JobType { get; set; } = "DataSync";
    public string Description { get; set; } = "";
    public string CronSchedule { get; set; } = "0 * * * *";
    public string? PayloadJson { get; set; }
}

// Entity Changes Audit Trail Models
public class EntityPropertyChangeDto
{
    public Guid Id { get; set; }
    public string PropertyName { get; set; } = "";
    public string? OriginalValue { get; set; }
    public string? NewValue { get; set; }
    public string PropertyType { get; set; } = "String";
}

public class EntityChangeDto
{
    public Guid Id { get; set; }
    public Guid? AuditLogId { get; set; }
    public string EntityType { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string ChangeType { get; set; } = "Updated";
    public DateTime ChangeTime { get; set; }
    public string? UserName { get; set; }
    public string? TenantName { get; set; }
    public List<EntityPropertyChangeDto> PropertyChanges { get; set; } = new();
}

// System Health & Diagnostics Models
public class SystemHealthDto
{
    public string Status { get; set; } = "Healthy";
    public string AppVersion { get; set; } = "2.4.0";
    public string DotNetVersion { get; set; } = "10.0";
    public string Environment { get; set; } = "Production";
    public TimeSpan Uptime { get; set; }
    public long MemoryAllocatedBytes { get; set; }
    public int ThreadCount { get; set; }
    public int Gen0Collections { get; set; }
    public int Gen1Collections { get; set; }
    public int Gen2Collections { get; set; }
    public string DatabaseStatus { get; set; } = "Operational";
    public long DatabaseLatencyMs { get; set; }
    public string OllamaStatus { get; set; } = "Online";
    public string ActiveModel { get; set; } = "gemma4:31b";
    public long StorageUsedBytes { get; set; }
    public long StorageTotalBytes { get; set; }
}

// Enterprise File Management Models
public class StoredFileDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long FileSizeBytes { get; set; }
    public string StorageProvider { get; set; } = "Local Disk";
    public string Category { get; set; } = "Documents";
    public string? Description { get; set; }
    public string? StoragePath { get; set; }
    public string UploadedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class CreateFileRequest
{
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "application/pdf";
    public long FileSizeBytes { get; set; } = 1048576;
    public string StorageProvider { get; set; } = "Local Disk";
    public string Category { get; set; } = "Documents";
    public string? Description { get; set; }
}

// Enterprise Notifications Models
public class NotificationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Severity { get; set; } = "Info";
    public string TargetAudience { get; set; } = "All Users";
    public bool IsRead { get; set; }
    public string? ActionUrl { get; set; }
    public string CreatedBy { get; set; } = "System";
    public DateTime CreatedAt { get; set; }
}

public class CreateNotificationRequest
{
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Severity { get; set; } = "Info";
    public string TargetAudience { get; set; } = "All Users";
    public string? ActionUrl { get; set; }
}

// Enterprise Text Templates Models
public class TextTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Category { get; set; } = "Email";
    public string Culture { get; set; } = "en-US";
    public string Subject { get; set; } = "";
    public string Content { get; set; } = "";
    public string AvailableTokens { get; set; } = "";
    public DateTime LastModifiedAt { get; set; }
}

public class CreateTemplateRequest
{
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Category { get; set; } = "Email";
    public string Culture { get; set; } = "en-US";
    public string Subject { get; set; } = "";
    public string Content { get; set; } = "";
    public string AvailableTokens { get; set; } = "";
}

public class UpdateTemplateRequest
{
    public string Subject { get; set; } = "";
    public string Content { get; set; } = "";
}

public class PreviewTemplateRequest
{
    public Dictionary<string, string> SampleValues { get; set; } = new();
}

public class TemplatePreviewResponse
{
    public string RenderedSubject { get; set; } = "";
    public string RenderedContent { get; set; } = "";
}

// Enterprise Localization Models
public class LanguageDto
{
    public Guid Id { get; set; }
    public string CultureCode { get; set; } = "en-US";
    public string DisplayName { get; set; } = "";
    public string FlagEmoji { get; set; } = "🌐";
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public int TranslationProgressPercent { get; set; } = 100;
}

public class CreateLanguageRequest
{
    public string CultureCode { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string FlagEmoji { get; set; } = "🌐";
}

public class LocalizationResourceDto
{
    public Guid Id { get; set; }
    public string ResourceKey { get; set; } = "";
    public string CultureCode { get; set; } = "en-US";
    public string Value { get; set; } = "";
    public string Category { get; set; } = "General";
    public DateTime LastModifiedAt { get; set; }
}

public class UpdateResourceRequest
{
    public string Value { get; set; } = "";
}

// Enterprise OAuth Applications Models
public class OAuthClientDto
{
    public Guid Id { get; set; }
    public string ClientId { get; set; } = "";
    public string ClientName { get; set; } = "";
    public string ClientType { get; set; } = "Confidential";
    public string AllowedGrantTypes { get; set; } = "authorization_code";
    public string AllowedScopes { get; set; } = "openid, profile, api";
    public string RedirectUris { get; set; } = "";
    public string? PostLogoutRedirectUris { get; set; }
    public string ClientSecretMasked { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public class CreateOAuthClientRequest
{
    public string ClientId { get; set; } = "";
    public string ClientName { get; set; } = "";
    public string ClientType { get; set; } = "Confidential";
    public string AllowedGrantTypes { get; set; } = "authorization_code, refresh_token";
    public string AllowedScopes { get; set; } = "openid, profile, email, api";
    public string RedirectUris { get; set; } = "https://localhost:5001/signin-oidc";
    public string? PostLogoutRedirectUris { get; set; }
}

public class UpdateOAuthClientRequest
{
    public string ClientName { get; set; } = "";
    public string AllowedGrantTypes { get; set; } = "";
    public string AllowedScopes { get; set; } = "";
    public string RedirectUris { get; set; } = "";
    public string? PostLogoutRedirectUris { get; set; }
    public bool IsActive { get; set; } = true;
}

// Aspire Cloud & Distributed Telemetry Models
public class CloudResourceDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string ResourceType { get; set; } = "Project";
    public string EndpointUrl { get; set; } = "";
    public string State { get; set; } = "Healthy";
    public int ReplicaCount { get; set; } = 1;
    public int MemoryMb { get; set; } = 128;
    public double CpuPercentage { get; set; } = 1.0;
    public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;
}

public class DistributedOrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public string ServiceOrigin { get; set; } = "";
    public string DestinationService { get; set; } = "";
    public string Status { get; set; } = "Completed";
    public decimal TotalAmount { get; set; }
    public long LatencyMs { get; set; }
    public string TraceId { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class DistributedTraceRecordDto
{
    public int Id { get; set; }
    public string TraceId { get; set; } = "";
    public string SpanId { get; set; } = "";
    public string ServiceName { get; set; } = "";
    public string OperationName { get; set; } = "";
    public long DurationMs { get; set; }
    public int StatusCode { get; set; } = 200;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateDistributedOrderRequest
{
    public string ServiceOrigin { get; set; } = "webfrontend";
    public string DestinationService { get; set; } = "apiservice";
    public decimal TotalAmount { get; set; } = 250.00m;
}

public class FaultSimulationRequest
{
    public string FaultType { get; set; } = "High Latency (1500ms)";
    public int LatencyMs { get; set; } = 1500;
    public bool TriggerFailure { get; set; } = false;
}