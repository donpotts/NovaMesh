namespace NovaMesh.Api.Models;

public record LoginRequest(string UserName, string Password);
public record RegisterRequest(string UserName, string? Email, string Password, string? FullName);
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
public record AppSettingDto(Guid Id, string Category, string Key, string Value, string Description, DateTime LastModifiedAt);
public record UpdateSettingRequest(string Key, string Value);

// SaaS Multi-Tenancy DTOs
public record TenantDto(Guid Id, string Name, string NormalizedName, string? ConnectionString, string EditionName, bool IsActive, string PrimaryColor, string SecondaryColor, string AccentColor, string? LogoUrl, string? CustomDomain, string? Slogan, DateTime CreatedAt);
public record CreateTenantRequest(string Name, string EditionName, string? ConnectionString, string? PrimaryColor = null, string? SecondaryColor = null, string? AccentColor = null, string? LogoUrl = null, string? CustomDomain = null, string? Slogan = null);
public record UpdateTenantRequest(string Name, string EditionName, bool IsActive, string? ConnectionString, string? PrimaryColor = null, string? SecondaryColor = null, string? AccentColor = null, string? LogoUrl = null, string? CustomDomain = null, string? Slogan = null);

// Feature Management DTOs
public record FeatureValueDto(Guid Id, string Name, string Value, string ProviderName, string? ProviderKey, string Description);
public record UpdateFeatureRequest(string Value);

// CMS Kit DTOs
public record BlogDto(Guid Id, string Name, string Slug, string Description, DateTime CreatedAt);
public record BlogPostDto(Guid Id, Guid BlogId, string Title, string Slug, string ShortDescription, string Content, string CoverImageUrl, string AuthorName, string AuthorAvatarUrl, string Category, string Tags, int ReadMinutes, int ViewCount, int LikeCount, bool IsPublished, DateTime CreatedAt, DateTime? PublishedAt);
public record CreateBlogPostRequest(string Title, string ShortDescription, string Content, string CoverImageUrl, string Category, string Tags, int ReadMinutes, bool IsPublished);
public record ReactRequest(string ReactionName);
public record PostCommentRequest(string AuthorName, string AuthorEmail, string CommentText);

// AI Knowledge Base, RAG, and Autonomous Agent DTOs
public record AiDocumentDto(Guid Id, string Title, string Category, string SourceType, string Summary, string Content, int ChunkCount, int Tokens, string EmbeddingModel, DateTime CreatedAt);
public record CreateAiDocumentRequest(string Title, string Category, string SourceType, string Content, string? Summary = null);
public record AiChunkDto(Guid Id, Guid DocumentId, int ChunkIndex, string Content, int TokenCount, string MetadataJson, double Similarity = 0);
public record RagCitationDto(string DocumentTitle, int ChunkIndex, double Similarity, string TextSnippet);
public record RagQueryRequest(string Query, double SimilarityThreshold = 0.70, int TopK = 4);
public record RagQueryResponse(string Query, string Answer, List<RagCitationDto> Citations, long ExecutionDurationMs, string GroundedPromptSnippet);
public record AiConversationDto(Guid Id, string Title, string Persona, string Model, DateTime CreatedAt, int MessageCount);
public record CreateConversationRequest(string Title, string Persona, string Model = "gemma4:31b");
public record AiToolCallDto(string ToolName, string Arguments, long DurationMs, string Result);
public record AiMessageDto(Guid Id, Guid ConversationId, string Role, string Content, List<RagCitationDto> Citations, List<AiToolCallDto> ToolCalls, DateTime CreatedAt);
public record SendAgentMessageRequest(string UserPrompt, string Persona = "Solution Architect", bool EnableRag = true, bool EnableAutonomousTools = true, string? Model = null);
public record AiAgentAuditLogDto(Guid Id, string AgentName, string ActionName, string InputPrompt, string OutputSummary, long ExecutionMs, bool Success, DateTime CreatedAt);

// Enterprise Dashboard & Analytics Telemetry DTOs
public record DashboardAnalyticsDto(
    int TotalUsers,
    int ActiveUsers,
    int TotalAuditLogs,
    long AvgLatencyMs,
    double SuccessRatePercent,
    int TotalAiDocs,
    int TotalRagChunks,
    int TotalAgentExecutions,
    string[] HourlyTrafficLabels,
    double[] RequestCounts,
    double[] LatencyMs,
    string[] StatusCodeLabels,
    double[] StatusCodeCounts,
    string[] AiUsageLabels,
    double[] AiTokensConsumed,
    double[] AiVectorQueries,
    string[] TenantDistributionLabels,
    double[] TenantCounts
);

// Enterprise Background Jobs & Worker Queue DTOs
public record BackgroundJobDto(
    Guid Id,
    string JobName,
    string JobGroup,
    string JobType,
    string Description,
    string CronSchedule,
    string Status,
    DateTime? LastRunTime,
    DateTime? NextRunTime,
    long ExecutionDurationMs,
    int RunCount,
    string? ErrorMessage,
    string? PayloadJson,
    DateTime CreatedAt
);

public record CreateBackgroundJobRequest(
    string JobName,
    string JobGroup,
    string JobType,
    string Description,
    string CronSchedule,
    string? PayloadJson
);

// Entity Changes Audit Trail DTOs
public record EntityPropertyChangeDto(
    Guid Id,
    string PropertyName,
    string? OriginalValue,
    string? NewValue,
    string PropertyType
);

public record EntityChangeDto(
    Guid Id,
    Guid? AuditLogId,
    string EntityType,
    string EntityId,
    string ChangeType,
    DateTime ChangeTime,
    string? UserName,
    string? TenantName,
    List<EntityPropertyChangeDto> PropertyChanges
);

// System Health & Diagnostics DTOs
public record SystemHealthDto(
    string Status,
    string AppVersion,
    string DotNetVersion,
    string Environment,
    TimeSpan Uptime,
    long MemoryAllocatedBytes,
    int ThreadCount,
    int Gen0Collections,
    int Gen1Collections,
    int Gen2Collections,
    string DatabaseStatus,
    long DatabaseLatencyMs,
    string OllamaStatus,
    string ActiveModel,
    long StorageUsedBytes,
    long StorageTotalBytes
);

// Enterprise File Management DTOs
public record StoredFileDto(
    Guid Id,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string StorageProvider,
    string Category,
    string? Description,
    string? StoragePath,
    string UploadedBy,
    DateTime CreatedAt
);

public record CreateFileRequest(
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string StorageProvider,
    string Category,
    string? Description
);

// Enterprise Notifications DTOs
public record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    string Severity,
    string TargetAudience,
    bool IsRead,
    string? ActionUrl,
    string CreatedBy,
    DateTime CreatedAt
);

public record CreateNotificationRequest(
    string Title,
    string Message,
    string Severity,
    string TargetAudience,
    string? ActionUrl
);

// Enterprise Text Templates DTOs
public record TextTemplateDto(
    Guid Id,
    string Name,
    string DisplayName,
    string Category,
    string Culture,
    string Subject,
    string Content,
    string AvailableTokens,
    DateTime LastModifiedAt
);

public record CreateTemplateRequest(
    string Name,
    string DisplayName,
    string Category,
    string Culture,
    string Subject,
    string Content,
    string AvailableTokens
);

public record UpdateTemplateRequest(
    string Subject,
    string Content
);

public record PreviewTemplateRequest(
    Dictionary<string, string> SampleValues
);

public record TemplatePreviewResponse(
    string RenderedSubject,
    string RenderedContent
);

// Enterprise Localization DTOs
public record LanguageDto(
    Guid Id,
    string CultureCode,
    string DisplayName,
    string FlagEmoji,
    bool IsDefault,
    bool IsActive,
    int TranslationProgressPercent
);

public record CreateLanguageRequest(
    string CultureCode,
    string DisplayName,
    string FlagEmoji
);

public record LocalizationResourceDto(
    Guid Id,
    string ResourceKey,
    string CultureCode,
    string Value,
    string Category,
    DateTime LastModifiedAt
);

public record UpdateResourceRequest(
    string Value
);

// Enterprise OAuth Applications DTOs
public record OAuthClientDto(
    Guid Id,
    string ClientId,
    string ClientName,
    string ClientType,
    string AllowedGrantTypes,
    string AllowedScopes,
    string RedirectUris,
    string? PostLogoutRedirectUris,
    string ClientSecretMasked,
    bool IsActive,
    DateTime CreatedAt
);

public record CreateOAuthClientRequest(
    string ClientId,
    string ClientName,
    string ClientType,
    string AllowedGrantTypes,
    string AllowedScopes,
    string RedirectUris,
    string? PostLogoutRedirectUris
);

public record UpdateOAuthClientRequest(
    string ClientName,
    string AllowedGrantTypes,
    string AllowedScopes,
    string RedirectUris,
    string? PostLogoutRedirectUris,
    bool IsActive
);

// Aspire Cloud & Distributed Telemetry DTOs
public record CloudResourceDto(int Id, string Name, string ResourceType, string EndpointUrl, string State, int ReplicaCount, int MemoryMb, double CpuPercentage, DateTime LastHeartbeat);
public record DistributedOrderDto(int Id, string OrderNumber, string ServiceOrigin, string DestinationService, string Status, decimal TotalAmount, long LatencyMs, string TraceId, DateTime CreatedAt);
public record DistributedTraceRecordDto(int Id, string TraceId, string SpanId, string ServiceName, string OperationName, long DurationMs, int StatusCode, string? ErrorMessage, DateTime CreatedAt);
public record CreateDistributedOrderRequest(string ServiceOrigin, string DestinationService, decimal TotalAmount);
public record FaultSimulationRequest(string FaultType, int LatencyMs, bool TriggerFailure);