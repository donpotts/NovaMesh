using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NovaMesh.Data.Entities;
using NovaMesh.Data.Security;

namespace NovaMesh.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db, string adminEmail = "admin@nexus.local", string adminPassword = "Admin123!", Action<string>? logger = null)
    {
        logger ??= _ => { };
        await db.Database.EnsureCreatedAsync();

        // 1. Roles & Permissions Tree
        if (!await db.Roles.AnyAsync())
        {
            logger("  [+] Seeding Application Roles & Permission Hierarchies");
            var roleAdmin = new AppRole { Name = "Admin", Description = "Cloud Solutions Architect - Full System Orchestration", PermissionsJson = JsonSerializer.Serialize(Permissions.All) };
            var roleDevOps = new AppRole { Name = "DevOps", Description = "DevOps & Infrastructure Engineer", PermissionsJson = JsonSerializer.Serialize(Permissions.Staff) };
            var roleSre = new AppRole { Name = "SRE", Description = "Site Reliability & Observability Lead", PermissionsJson = JsonSerializer.Serialize(Permissions.Staff) };
            var roleUser = new AppRole { Name = "User", Description = "Distributed Service Consumer", PermissionsJson = JsonSerializer.Serialize(Permissions.Standard) };
            db.Roles.AddRange(roleAdmin, roleDevOps, roleSre, roleUser);
            await db.SaveChangesAsync();

            // Seed Permission Grants
            if (!await db.PermissionGrants.AnyAsync())
            {
                logger("  [+] Seeding Hierarchical Permission Grants");
                var grants = new List<PermissionGrant>();
                foreach (var perm in Permissions.All)
                {
                    grants.Add(new PermissionGrant { RoleName = "Admin", PermissionKey = perm, IsGranted = true });
                }
                foreach (var perm in Permissions.Staff)
                {
                    grants.Add(new PermissionGrant { RoleName = "Staff", PermissionKey = perm, IsGranted = true });
                    grants.Add(new PermissionGrant { RoleName = "Editor", PermissionKey = perm, IsGranted = true });
                    grants.Add(new PermissionGrant { RoleName = "StoreManager", PermissionKey = perm, IsGranted = true });
                    grants.Add(new PermissionGrant { RoleName = "SalesManager", PermissionKey = perm, IsGranted = true });
                }
                foreach (var perm in Permissions.Standard)
                {
                    grants.Add(new PermissionGrant { RoleName = "User", PermissionKey = perm, IsGranted = true });
                    grants.Add(new PermissionGrant { RoleName = "Customer", PermissionKey = perm, IsGranted = true });
                }
                db.PermissionGrants.AddRange(grants);
                await db.SaveChangesAsync();
            }
        }

        // 2. Personas & Users
        if (!await db.Users.AnyAsync())
        {
            logger("  [+] Seeding Core Users & Persona Accounts");
            logger("  [+] Seeding Cloud Architect: admin");
            PasswordHasher.HashPassword("Admin123!", out var h1, out var s1);
            var uAdmin = new AppUser { UserName = "admin", Email = "admin@aspire.local", FullName = "Cloud Solutions Architect", PasswordHash = h1, Salt = s1, IsActive = true };

            logger("  [+] Seeding DevOps Engineer: devops");
            PasswordHasher.HashPassword("Devops123!", out var h2, out var s2);
            var uDevops = new AppUser { UserName = "devops", Email = "devops@aspire.local", FullName = "DevOps Lead", PasswordHash = h2, Salt = s2, IsActive = true };

            logger("  [+] Seeding SRE Observability Lead: sre");
            PasswordHasher.HashPassword("Sre123!", out var h3, out var s3);
            var uSre = new AppUser { UserName = "sre", Email = "sre@aspire.local", FullName = "SRE Observability Lead", PasswordHash = h3, Salt = s3, IsActive = true };

            logger("  [+] Seeding Standard User: user");
            PasswordHasher.HashPassword("User123!", out var h4, out var s4);
            var uUser = new AppUser { UserName = "user", Email = "user@aspire.local", FullName = "Distributed Service Viewer", PasswordHash = h4, Salt = s4, IsActive = true };

            db.Users.AddRange(uAdmin, uDevops, uSre, uUser);
            await db.SaveChangesAsync();

            var rAdmin = await db.Roles.FirstAsync(r => r.Name == "Admin");
            var rDevops = await db.Roles.FirstAsync(r => r.Name == "DevOps");
            var rSre = await db.Roles.FirstAsync(r => r.Name == "SRE");
            var rUser = await db.Roles.FirstAsync(r => r.Name == "User");

            db.UserRoles.AddRange(
                new AppUserRole { UserId = uAdmin.Id, RoleId = rAdmin.Id },
                new AppUserRole { UserId = uDevops.Id, RoleId = rDevops.Id },
                new AppUserRole { UserId = uSre.Id, RoleId = rSre.Id },
                new AppUserRole { UserId = uUser.Id, RoleId = rUser.Id }
            );
            await db.SaveChangesAsync();
        }

        // 3. System Settings Module
        if (!await db.Settings.AnyAsync())
        {
            logger("  [+] Seeding System Configuration Settings (General, Security, Email, Audit)");
            db.Settings.AddRange(
                new AppSetting { Category = "General", Key = "App.General.SiteName", Value = "NovaMesh Enterprise Portal", Description = "Global application name displayed in navigation bars and document titles" },
                new AppSetting { Category = "General", Key = "App.General.DefaultTheme", Value = "Dark", Description = "Default color theme for new guest visitors (Dark / Light)" },
                new AppSetting { Category = "General", Key = "App.General.DefaultPageSize", Value = "10", Description = "Default item pagination count on data tables" },
                new AppSetting { Category = "General", Key = "App.General.PublicRegistration", Value = "true", Description = "Allow public self-service user registration" },
                new AppSetting { Category = "Security", Key = "App.Security.PasswordMinLength", Value = "8", Description = "Minimum required length for user passwords" },
                new AppSetting { Category = "Security", Key = "App.Security.RequireUppercase", Value = "true", Description = "Require at least one uppercase letter in passwords" },
                new AppSetting { Category = "Security", Key = "App.Security.SessionTimeoutMinutes", Value = "60", Description = "Idle session timeout in minutes before requiring re-authentication" },
                new AppSetting { Category = "Email", Key = "App.Email.SmtpHost", Value = "smtp.example.com", Description = "Outbound SMTP relay hostname" },
                new AppSetting { Category = "Email", Key = "App.Email.SmtpPort", Value = "587", Description = "SMTP relay port number" },
                new AppSetting { Category = "Email", Key = "App.Email.SenderEmail", Value = "noreply@novamesh.com", Description = "Automated transactional email sender address" },
                new AppSetting { Category = "Audit", Key = "App.Audit.Enabled", Value = "true", Description = "Enable real-time audit logging middleware for all HTTP requests" },
                new AppSetting { Category = "Audit", Key = "App.Audit.RetentionDays", Value = "90", Description = "Historical audit log retention duration in days" }
            );
            await db.SaveChangesAsync();
        }

        // 4. Audit Trail History
        if (!await db.AuditLogs.AnyAsync())
        {
            logger("  [+] Seeding Live Audit Trail History (15+ HTTP Event Records)");
            var now = DateTime.UtcNow;
            db.AuditLogs.AddRange(
                new AuditLogEntry { ExecutionTime = now.AddHours(-46), UserName = "admin", HttpMethod = "POST", Url = "/api/auth/login", HttpStatusCode = 200, DurationMs = 45, ClientIpAddress = "192.168.1.10", BrowserInfo = "Mozilla/5.0 (Windows NT 10.0; Win64)", ActionName = "Authentication.Login" },
                new AuditLogEntry { ExecutionTime = now.AddHours(-44), UserName = "admin", HttpMethod = "GET", Url = "/api/settings", HttpStatusCode = 200, DurationMs = 18, ClientIpAddress = "192.168.1.10", BrowserInfo = "Mozilla/5.0 (Windows NT 10.0; Win64)", ActionName = "Settings.GetList" },
                new AuditLogEntry { ExecutionTime = now.AddHours(-40), UserName = "admin", HttpMethod = "PUT", Url = "/api/settings", HttpStatusCode = 200, DurationMs = 38, ClientIpAddress = "192.168.1.10", BrowserInfo = "Mozilla/5.0 (Windows NT 10.0; Win64)", ActionName = "Settings.Update" },
                new AuditLogEntry { ExecutionTime = now.AddHours(-36), UserName = "editor", HttpMethod = "POST", Url = "/api/auth/login", HttpStatusCode = 200, DurationMs = 42, ClientIpAddress = "10.0.0.15", BrowserInfo = "Mozilla/5.0 (Macintosh; Intel Mac OS X)", ActionName = "Authentication.Login" },
                new AuditLogEntry { ExecutionTime = now.AddHours(-32), UserName = "editor", HttpMethod = "POST", Url = "/api/cms/articles", HttpStatusCode = 201, DurationMs = 82, ClientIpAddress = "10.0.0.15", BrowserInfo = "Mozilla/5.0 (Macintosh; Intel Mac OS X)", ActionName = "Cms.CreateArticle" },
                new AuditLogEntry { ExecutionTime = now.AddHours(-28), UserName = "user", HttpMethod = "GET", Url = "/api/cms/articles?pageNumber=1&pageSize=10", HttpStatusCode = 200, DurationMs = 24, ClientIpAddress = "172.16.4.88", BrowserInfo = "Mozilla/5.0 (iPhone; CPU iPhone OS)", ActionName = "Cms.GetArticlesPaged" },
                new AuditLogEntry { ExecutionTime = now.AddHours(-24), UserName = "user", HttpMethod = "POST", Url = "/api/cms/comments", HttpStatusCode = 201, DurationMs = 55, ClientIpAddress = "172.16.4.88", BrowserInfo = "Mozilla/5.0 (iPhone; CPU iPhone OS)", ActionName = "Cms.PostComment" },
                new AuditLogEntry { ExecutionTime = now.AddHours(-18), UserName = "admin", HttpMethod = "GET", Url = "/api/identity/users", HttpStatusCode = 200, DurationMs = 28, ClientIpAddress = "192.168.1.10", BrowserInfo = "Mozilla/5.0 (Windows NT 10.0; Win64)", ActionName = "Identity.GetUsers" },
                new AuditLogEntry { ExecutionTime = now.AddHours(-14), UserName = "admin", HttpMethod = "GET", Url = "/api/audit-logs", HttpStatusCode = 200, DurationMs = 32, ClientIpAddress = "192.168.1.10", BrowserInfo = "Mozilla/5.0 (Windows NT 10.0; Win64)", ActionName = "AuditLogging.GetLogs" },
                new AuditLogEntry { ExecutionTime = now.AddHours(-10), UserName = "publisher", HttpMethod = "POST", Url = "/api/auth/login", HttpStatusCode = 200, DurationMs = 39, ClientIpAddress = "10.0.0.22", BrowserInfo = "Mozilla/5.0 (X11; Linux x86_64)", ActionName = "Authentication.Login" },
                new AuditLogEntry { ExecutionTime = now.AddHours(-6), UserName = "publisher", HttpMethod = "PUT", Url = "/api/cms/articles/1", HttpStatusCode = 200, DurationMs = 48, ClientIpAddress = "10.0.0.22", BrowserInfo = "Mozilla/5.0 (X11; Linux x86_64)", ActionName = "Cms.UpdateArticle" },
                new AuditLogEntry { ExecutionTime = now.AddHours(-2), UserName = "admin", HttpMethod = "GET", Url = "/api/account/my-profile", HttpStatusCode = 200, DurationMs = 12, ClientIpAddress = "192.168.1.10", BrowserInfo = "Mozilla/5.0 (Windows NT 10.0; Win64)", ActionName = "Account.GetMyProfile" },
                new AuditLogEntry { ExecutionTime = now.AddMinutes(-30), UserName = "user", HttpMethod = "GET", Url = "/api/cms/categories", HttpStatusCode = 200, DurationMs = 15, ClientIpAddress = "172.16.4.88", BrowserInfo = "Mozilla/5.0 (iPhone; CPU iPhone OS)", ActionName = "Cms.GetCategories" },
                new AuditLogEntry { ExecutionTime = now.AddMinutes(-10), UserName = "admin", HttpMethod = "GET", Url = "/scalar/v1", HttpStatusCode = 200, DurationMs = 8, ClientIpAddress = "127.0.0.1", BrowserInfo = "ScalarClient/2.17", ActionName = "OpenApi.ScalarDocs" }
            );
            await db.SaveChangesAsync();
        }

        // 5. SaaS Multi-Tenancy Module
        if (!await db.Tenants.AnyAsync())
        {
            logger("  [+] Seeding SaaS Multi-Tenancy Tenants & Modern Enterprise Branding");
            db.Tenants.AddRange(
                new Tenant
                {
                    Name = "Acme Global Dynamics",
                    NormalizedName = "ACME-GLOBAL",
                    EditionName = "Enterprise",
                    IsActive = true,
                    ConnectionString = null,
                    PrimaryColor = "#4F46E5",
                    SecondaryColor = "#06B6D4",
                    AccentColor = "#F59E0B",
                    CustomDomain = "acme.portal.local",
                    Slogan = "Accelerating Global Enterprise Intelligence"
                },
                new Tenant
                {
                    Name = "Stark Logistics",
                    NormalizedName = "STARK-LOGISTICS",
                    EditionName = "Standard",
                    IsActive = true,
                    ConnectionString = null,
                    PrimaryColor = "#059669",
                    SecondaryColor = "#10B981",
                    AccentColor = "#3B82F6",
                    CustomDomain = "logistics.stark.local",
                    Slogan = "Ultra-Fast Autonomous Global Fulfillment"
                },
                new Tenant
                {
                    Name = "Cyberdyne Systems",
                    NormalizedName = "CYBERDYNE",
                    EditionName = "Professional",
                    IsActive = true,
                    ConnectionString = null,
                    PrimaryColor = "#E11D48",
                    SecondaryColor = "#9333EA",
                    AccentColor = "#F97316",
                    CustomDomain = "cyberdyne.ai.local",
                    Slogan = "Next-Generation Neural Computing & Robotics"
                },
                new Tenant
                {
                    Name = "Apex Cloud Systems",
                    NormalizedName = "APEX-CLOUD",
                    EditionName = "Enterprise",
                    IsActive = true,
                    ConnectionString = null,
                    PrimaryColor = "#0284C7",
                    SecondaryColor = "#38BDF8",
                    AccentColor = "#10B981",
                    CustomDomain = "cloud.apex.local",
                    Slogan = "Mission-Critical Infrastructure Orchestration"
                }
            );
            await db.SaveChangesAsync();
        }

        // 6. Feature Management Module
        if (!await db.FeatureValues.AnyAsync())
        {
            logger("  [+] Seeding Feature Management Definitions & Flags");
            db.FeatureValues.AddRange(
                new FeatureValue { Name = "CmsKit.Blogs.Enabled", Value = "true", ProviderName = "Global", Description = "Enables multi-blog authoring and public reading platform" },
                new FeatureValue { Name = "CmsKit.Comments.Enabled", Value = "true", ProviderName = "Global", Description = "Enables interactive community commenting on blogs & articles" },
                new FeatureValue { Name = "CmsKit.Reactions.Enabled", Value = "true", ProviderName = "Global", Description = "Enables reader reactions (Like, Heart, Clap) on content" },
                new FeatureValue { Name = "CmsKit.Tags.Enabled", Value = "true", ProviderName = "Global", Description = "Enables tag taxonomy and search filtering" },
                new FeatureValue { Name = "CmsKit.Pages.Enabled", Value = "true", ProviderName = "Global", Description = "Enables dynamic CMS static pages engine" },
                new FeatureValue { Name = "AuditLogging.RealTime.Enabled", Value = "true", ProviderName = "Global", Description = "Real-time HTTP request interceptor logging" },
                new FeatureValue { Name = "Identity.TwoFactor.Enabled", Value = "false", ProviderName = "Global", Description = "Enforce two-factor authentication for administrative users" },
                new FeatureValue { Name = "MultiTenancy.DatabasePerTenant.Enabled", Value = "false", ProviderName = "Global", Description = "Tenant dedicated database isolation mode" }
            );
            await db.SaveChangesAsync();
        }

        // 8. AI Knowledge Base & RAG Ingestion Module
        if (!await db.AiDocuments.AnyAsync())
        {
            logger("  [+] Seeding Enterprise AI Document Knowledge Base & Vector RAG Chunks");
            var doc1Id = Guid.Parse("f1111111-aaaa-bbbb-cccc-111111111111");
            var doc2Id = Guid.Parse("f2222222-aaaa-bbbb-cccc-222222222222");
            var doc3Id = Guid.Parse("f3333333-aaaa-bbbb-cccc-333333333333");
            var doc4Id = Guid.Parse("f4444444-aaaa-bbbb-cccc-444444444444");

            var doc1 = new AiDocument
            {
                Id = doc1Id,
                Title = "My Platform Modular Architecture & Clean Boundaries",
                Category = "Architecture",
                SourceType = "Architecture Spec",
                Content = "My Platform structures enterprise software into decoupled layers. The Domain layer hosts Aggregate Roots, Entities, Value Objects, and Domain Services that enforce business rules without external dependencies. The Contracts layer defines interfaces and DTOs. The Application layer orchestrates business workflows. The HttpApi layer exposes ASP.NET Core minimal APIs and OpenAPI specs. MudBlazor WASM acts as the presentation layer consuming strongly typed contracts.",
                TokenCount = 1250,
                EmbeddingModel = "nomic-embed-text:latest"
            };

            var doc2 = new AiDocument
            {
                Id = doc2Id,
                Title = "Vector RAG Ingestion & Semantic Retrieval Engine",
                Category = "AI & RAG",
                SourceType = "Markdown",
                Content = "The RAG engine decomposes ingested documents into semantically coherent text chunks. Each chunk is projected into high-dimensional vector space using embedding models. When a user submits an inquiry, the query is embedded and evaluated against stored chunk vectors via cosine similarity. Top-k chunks exceeding the similarity threshold are synthesized into a grounded prompt context with explicit citation references.",
                TokenCount = 1480,
                EmbeddingModel = "nomic-embed-text:latest"
            };

            var doc3 = new AiDocument
            {
                Id = doc3Id,
                Title = "Autonomous AI Agent Tool Calling & Plan-and-Solve Protocol",
                Category = "Autonomous Agents",
                SourceType = "Manual",
                Content = "Our Autonomous AI Agent uses iterative tool-calling loops. The agent inspects user prompts, selects relevant registered tools (e.g., QueryKnowledgeBase, InspectSystemHealth, SynthesizeMudBlazorCode, AuditTenantSecurity), executes them with strict validation, observes the returned telemetry, and iteratively refines its solution before delivering final answers.",
                TokenCount = 980,
                EmbeddingModel = "nomic-embed-text:latest"
            };

            var doc4 = new AiDocument
            {
                Id = doc4Id,
                Title = "Multi-Tenant Security Isolation & Dynamic Theming",
                Category = "Security & Multi-Tenancy",
                SourceType = "Architecture Spec",
                Content = "Multi-tenancy is enforced through ambient TenantState resolved on every HTTP request and WebAssembly client session. Data isolation is maintained via EF Core Global Query Filters. Theming is dynamically synchronized in real-time, allowing tenants to customize Primary, Secondary, and Accent palettes across all UI components.",
                TokenCount = 890,
                EmbeddingModel = "nomic-embed-text:latest"
            };

            db.AiDocuments.AddRange(doc1, doc2, doc3, doc4);
            await db.SaveChangesAsync();

            // Seed Chunks with simulated high-dimensional vectors
            db.AiChunks.AddRange(
                new AiDocumentChunk
                {
                    DocumentId = doc1Id,
                    ChunkIndex = 0,
                    Content = "Domain Layer: Aggregate Roots safeguard domain invariants and emit distributed domain events. Entities use strongly typed IDs and maintain business logic purity without database dependencies.",
                    EmbeddingVectorJson = "[0.124, 0.854, -0.421, 0.312, 0.055, 0.671]"
                },
                new AiDocumentChunk
                {
                    DocumentId = doc1Id,
                    ChunkIndex = 1,
                    Content = "Application Services & Contracts: DTOs decouple domain entities from external clients. Application services execute transactions, validate commands, and map results to response models.",
                    EmbeddingVectorJson = "[0.231, 0.642, -0.115, 0.528, 0.189, 0.442]"
                },
                new AiDocumentChunk
                {
                    DocumentId = doc1Id,
                    ChunkIndex = 2,
                    Content = "HttpApi & Scalar OpenAPI: Endpoints are mapped via high-throughput ASP.NET Core minimal APIs. Scalar interactive UI provides modern Mars-themed documentation and interactive client testing.",
                    EmbeddingVectorJson = "[0.412, 0.318, -0.224, 0.731, 0.298, 0.115]"
                },
                new AiDocumentChunk
                {
                    DocumentId = doc2Id,
                    ChunkIndex = 0,
                    Content = "Chunking and Tokenization: Ingestion splits source files by markdown headers and sentence boundaries with 15% sliding window overlap to preserve semantic context across chunk edges.",
                    EmbeddingVectorJson = "[0.672, 0.119, 0.442, -0.321, 0.589, 0.201]"
                },
                new AiDocumentChunk
                {
                    DocumentId = doc2Id,
                    ChunkIndex = 1,
                    Content = "Cosine Similarity & Top-K Ranking: Queries are embedded and compared using normalized dot products. Only chunks scoring above threshold 0.70 are selected for prompt augmentation.",
                    EmbeddingVectorJson = "[0.715, 0.218, 0.512, -0.219, 0.631, 0.312]"
                },
                new AiDocumentChunk
                {
                    DocumentId = doc2Id,
                    ChunkIndex = 2,
                    Content = "Citations & Attribution: Every assertion generated by the RAG model is anchored to a Document Title, Section Heading, and Chunk Index with confidence scores.",
                    EmbeddingVectorJson = "[0.589, 0.342, 0.398, -0.187, 0.478, 0.405]"
                },
                new AiDocumentChunk
                {
                    DocumentId = doc3Id,
                    ChunkIndex = 0,
                    Content = "Autonomous Agent Tool Calling: The agent executes registered C# tool delegates dynamically. Tools include QueryKnowledgeBase, InspectSystemHealth, SynthesizeMudBlazorCode, and AuditTenantSecurity.",
                    EmbeddingVectorJson = "[0.328, 0.451, 0.612, 0.218, -0.198, 0.572]"
                },
                new AiDocumentChunk
                {
                    DocumentId = doc3Id,
                    ChunkIndex = 1,
                    Content = "Multi-Persona Specialization: Personas configure temperature, system prompts, and tool access matrices for Architecture, Code Synthesis, Security, and Domain Support.",
                    EmbeddingVectorJson = "[0.395, 0.512, 0.489, 0.176, -0.098, 0.621]"
                },
                new AiDocumentChunk
                {
                    DocumentId = doc4Id,
                    ChunkIndex = 0,
                    Content = "Tenant Context Resolution: Current tenant is determined from host subdomains, headers, or client state. Global query filters ensure zero data contamination across tenants.",
                    EmbeddingVectorJson = "[0.189, 0.721, -0.312, 0.415, 0.112, 0.398]"
                },
                new AiDocumentChunk
                {
                    DocumentId = doc4Id,
                    ChunkIndex = 1,
                    Content = "Dynamic Theme Branding: Tenants configure modern hex color tokens (Primary, Secondary, Accent). MudThemeProvider reactively updates color palettes across the entire app.",
                    EmbeddingVectorJson = "[0.215, 0.689, -0.298, 0.389, 0.201, 0.451]"
                }
            );
            await db.SaveChangesAsync();

            // Seed AI Agent Conversation with RAG Citations and Tool Traces
            var convId = Guid.Parse("c1111111-aaaa-bbbb-cccc-111111111111");
            var conv = new AiAgentConversation
            {
                Id = convId,
                Title = "Enterprise Solution Architecture & RAG Review",
                Persona = "Solution Architect",
                Model = "gemma4:31b",
                CreatedAt = DateTime.UtcNow.AddHours(-3)
            };
            db.AiConversations.Add(conv);
            await db.SaveChangesAsync();

            db.AiMessages.AddRange(
                new AiAgentMessage
                {
                    ConversationId = convId,
                    Role = "user",
                    Content = "How does our RAG pipeline integrate with the My Platform modular architecture, and how does the Autonomous Agent execute tools?",
                    CreatedAt = DateTime.UtcNow.AddHours(-3).AddMinutes(1)
                },
                new AiAgentMessage
                {
                    ConversationId = convId,
                    Role = "assistant",
                    Content = "### Architectural Synthesis\n\nOur system bridges **My Platform Clean Architecture** and **Modern AI RAG** with autonomous tool calling:\n\n1. **Modular Boundary Decoupling**: Knowledge ingestion operates as an independent bounded context. Domain documents (Architecture Specs, Markdown manuals, and OpenAPI schemas) are tokenized and stored in `AiDocuments` and `AiChunks`.\n2. **Vector RAG Retrieval**: When you query the system, the query vector is scored against all stored chunk embeddings using cosine similarity. Only chunks exceeding the 0.72 threshold are augmented into the LLM context, accompanied by explicit source citations.\n3. **Autonomous Tool Calling Execution**: The agent uses an autonomous execution loop to inspect parameters, invoke backend tools (such as `QueryKnowledgeBase` and `InspectSystemMetrics`), verify responses, and return structured answers with full audit telemetry.\n\n```csharp\n// Example of RAG Semantic Retrieval Execution\nvar topChunks = await ragEngine.RetrieveSimilarChunksAsync(queryVector, threshold: 0.72, topK: 5);\nvar response = await agent.SynthesizeWithCitationsAsync(query, topChunks);\n```",
                    CitationsJson = "[{\"documentTitle\":\"My Platform Modular Architecture & Clean Boundaries\",\"chunkIndex\":0,\"similarity\":0.94,\"textSnippet\":\"Domain Layer: Aggregate Roots safeguard domain invariants and emit distributed domain events.\"},{\"documentTitle\":\"Vector RAG Ingestion & Semantic Retrieval Engine\",\"chunkIndex\":1,\"similarity\":0.91,\"textSnippet\":\"Cosine Similarity & Top-K Ranking: Queries are embedded and compared using normalized dot products.\"},{\"documentTitle\":\"Autonomous AI Agent Tool Calling & Plan-and-Solve Protocol\",\"chunkIndex\":0,\"similarity\":0.88,\"textSnippet\":\"Autonomous Agent Tool Calling: The agent executes registered C# tool delegates dynamically.\"}]",
                    ToolCallsJson = "[{\"toolName\":\"QueryKnowledgeBase\",\"arguments\":\"{\\\"query\\\": \\\"My Platform modular RAG tool calling\\\", \\\"topK\\\": 3}\",\"durationMs\":34,\"result\":\"3 matching chunks retrieved with similarity > 0.88\"},{\"toolName\":\"InspectSystemHealth\",\"arguments\":\"{}\",\"durationMs\":12,\"result\":\"All 12 modules healthy, latency 18ms\"}]",
                    CreatedAt = DateTime.UtcNow.AddHours(-3).AddMinutes(2)
                }
            );
            await db.SaveChangesAsync();

            // Seed Agent Audit Logs
            db.AiAgentAuditLogs.AddRange(
                new AiAgentAuditLog { AgentName = "Solution Architect Agent", ActionName = "Semantic Vector Search", InputPrompt = "My Platform modular RAG tool calling", OutputSummary = "Retrieved 3 chunks from Knowledge Base with similarity 0.94, 0.91, 0.88", ExecutionMs = 34, Success = true, CreatedAt = DateTime.UtcNow.AddHours(-3).AddMinutes(1) },
                new AiAgentAuditLog { AgentName = "Solution Architect Agent", ActionName = "System Telemetry Tool Call", InputPrompt = "InspectSystemHealth", OutputSummary = "Verified 12 active My Platform enterprise modules and database integrity", ExecutionMs = 12, Success = true, CreatedAt = DateTime.UtcNow.AddHours(-3).AddMinutes(2) },
                new AiAgentAuditLog { AgentName = "Code Synthesizer Agent", ActionName = "Code Generation", InputPrompt = "Generate MudBlazor Tenant Switcher", OutputSummary = "Generated MainLayout responsive top nav component with reactive state hooks", ExecutionMs = 58, Success = true, CreatedAt = DateTime.UtcNow.AddHours(-1) }
            );
            await db.SaveChangesAsync();

            // Background Jobs & Worker Queue Seed
            if (!await db.BackgroundJobs.AnyAsync())
            {
                db.BackgroundJobs.AddRange(
                    new BackgroundJobRecord
                    {
                        JobName = "AI Vector Index Sync & Warmup",
                        JobGroup = "AI Suite",
                        JobType = "AiVectorIndexWarmup",
                        Description = "Incremental re-embedding of changed documentation chunks into memory cache and vector database.",
                        CronSchedule = "*/15 * * * *",
                        Status = "Active",
                        LastRunTime = DateTime.UtcNow.AddMinutes(-12),
                        NextRunTime = DateTime.UtcNow.AddMinutes(3),
                        ExecutionDurationMs = 1250,
                        RunCount = 184,
                        PayloadJson = "{\"targetCollection\": \"KnowledgeBase_v1\", \"batchSize\": 64, \"models\": [\"gemma4:31b\", \"nomic-embed-text\"]}"
                    },
                    new BackgroundJobRecord
                    {
                        JobName = "Audit Log Auto-Archiving & Pruning",
                        JobGroup = "Maintenance",
                        JobType = "AuditLogPruning",
                        Description = "Pruning audit records older than 90 days into cold compressed storage.",
                        CronSchedule = "0 2 * * *",
                        Status = "Completed",
                        LastRunTime = DateTime.UtcNow.AddHours(-2),
                        NextRunTime = DateTime.UtcNow.AddHours(22),
                        ExecutionDurationMs = 420,
                        RunCount = 42,
                        PayloadJson = "{\"retentionDays\": 90, \"archiveDestination\": \"s3://nexus-audit-cold/\"}"
                    },
                    new BackgroundJobRecord
                    {
                        JobName = "Multi-Tenant Resource Quota Enforcer",
                        JobGroup = "SaaS",
                        JobType = "TenantQuotaEnforcer",
                        Description = "Audits tenant user quotas, file storage limits, and AI inference credit balances.",
                        CronSchedule = "0 0 * * *",
                        Status = "Active",
                        LastRunTime = DateTime.UtcNow.AddHours(-14),
                        NextRunTime = DateTime.UtcNow.AddHours(10),
                        ExecutionDurationMs = 840,
                        RunCount = 30,
                        PayloadJson = "{\"enforceThreshold\": 0.95, \"alertEmail\": \"ops@nexus.local\"}"
                    },
                    new BackgroundJobRecord
                    {
                        JobName = "Daily System Digest & Alert Notification Dispatcher",
                        JobGroup = "Notifications",
                        JobType = "EmailDigestSender",
                        Description = "Aggregates unread security notices, high-latency alerts, and sends daily digest to tenant administrators.",
                        CronSchedule = "0 8 * * *",
                        Status = "Active",
                        LastRunTime = DateTime.UtcNow.AddHours(-6),
                        NextRunTime = DateTime.UtcNow.AddHours(18),
                        ExecutionDurationMs = 310,
                        RunCount = 15,
                        PayloadJson = "{\"template\": \"Digest.DailySummary\", \"priority\": \"High\"}"
                    },
                    new BackgroundJobRecord
                    {
                        JobName = "Database Vacuum & Index Optimization",
                        JobGroup = "Maintenance",
                        JobType = "DataSync",
                        Description = "SQLite / Relational DB defragmentation, WAL checkpointing, and execution plan optimization.",
                        CronSchedule = "0 4 * * 0",
                        Status = "Paused",
                        LastRunTime = DateTime.UtcNow.AddDays(-3),
                        NextRunTime = DateTime.UtcNow.AddDays(4),
                        ExecutionDurationMs = 3400,
                        RunCount = 8,
                        PayloadJson = "{\"vacuum\": true, \"analyze\": true}"
                    }
                );
                await db.SaveChangesAsync();
            }

            // Entity Property Audit History & Change Tracking Seed
            if (!await db.EntityChanges.AnyAsync())
            {
                var change1 = new EntityChangeRecord
                {
                    EntityType = "Tenant",
                    EntityId = "33333333-3333-3333-3333-333333333333",
                    ChangeType = "Updated",
                    UserName = "admin@nexus.local",
                    TenantName = "Cyberdyne Systems",
                    ChangeTime = DateTime.UtcNow.AddHours(-1)
                };
                change1.PropertyChanges.AddRange(
                    new EntityPropertyChangeRecord { EntityChange = change1, PropertyName = "EditionName", OriginalValue = "Professional", NewValue = "Enterprise", PropertyType = "String" },
                    new EntityPropertyChangeRecord { EntityChange = change1, PropertyName = "PrimaryColor", OriginalValue = "#3B82F6", NewValue = "#DC2626", PropertyType = "HexColor" },
                    new EntityPropertyChangeRecord { EntityChange = change1, PropertyName = "AccentColor", OriginalValue = "#10B981", NewValue = "#991B1B", PropertyType = "HexColor" },
                    new EntityPropertyChangeRecord { EntityChange = change1, PropertyName = "Slogan", OriginalValue = "Autonomous Neural Infrastructure", NewValue = "Autonomous Defense Systems & Neural Dynamics", PropertyType = "String" }
                );

                var change2 = new EntityChangeRecord
                {
                    EntityType = "AppUser",
                    EntityId = "11111111-1111-1111-1111-111111111111",
                    ChangeType = "Updated",
                    UserName = "system_security",
                    TenantName = "Acme Global Dynamics",
                    ChangeTime = DateTime.UtcNow.AddHours(-2).AddMinutes(-15)
                };
                change2.PropertyChanges.AddRange(
                    new EntityPropertyChangeRecord { EntityChange = change2, PropertyName = "Roles", OriginalValue = "Editor", NewValue = "Admin, Publisher, Editor", PropertyType = "List<String>" },
                    new EntityPropertyChangeRecord { EntityChange = change2, PropertyName = "IsActive", OriginalValue = "false", NewValue = "true", PropertyType = "Boolean" },
                    new EntityPropertyChangeRecord { EntityChange = change2, PropertyName = "TwoFactorEnabled", OriginalValue = "false", NewValue = "true", PropertyType = "Boolean" }
                );

                var change3 = new EntityChangeRecord
                {
                    EntityType = "AppSetting",
                    EntityId = "44444444-4444-4444-4444-444444444444",
                    ChangeType = "Updated",
                    UserName = "admin@nexus.local",
                    TenantName = "Stark Logistics",
                    ChangeTime = DateTime.UtcNow.AddHours(-3).AddMinutes(-40)
                };
                change3.PropertyChanges.AddRange(
                    new EntityPropertyChangeRecord { EntityChange = change3, PropertyName = "Value", OriginalValue = "Light", NewValue = "Dark", PropertyType = "String" },
                    new EntityPropertyChangeRecord { EntityChange = change3, PropertyName = "LastModifiedBy", OriginalValue = "null", NewValue = "admin@nexus.local", PropertyType = "String" }
                );

                db.EntityChanges.AddRange(change1, change2, change3);
                await db.SaveChangesAsync();
            }

            // 7. Enterprise File Management Seed
            if (!await db.Files.AnyAsync())
            {
                db.Files.AddRange(
                    new StoredFile
                    {
                        FileName = "Annual_Financial_Report_2026.pdf",
                        ContentType = "application/pdf",
                        FileSizeBytes = 4210500,
                        StorageProvider = "Local Disk",
                        Category = "Documents",
                        Description = "Consolidated audited GAAP financial statements and balance sheets.",
                        StoragePath = "uploads/documents/Annual_Financial_Report_2026.pdf",
                        UploadedBy = "cfo@platform.local",
                        CreatedAt = DateTime.UtcNow.AddDays(-12)
                    },
                    new StoredFile
                    {
                        FileName = "Platform_Architecture_Blueprint.png",
                        ContentType = "image/png",
                        FileSizeBytes = 1840200,
                        StorageProvider = "AWS S3",
                        Category = "Images",
                        Description = "High-resolution decoupled distributed architecture diagram.",
                        StoragePath = "s3://platform-assets/images/Platform_Architecture_Blueprint.png",
                        UploadedBy = "architect@platform.local",
                        CreatedAt = DateTime.UtcNow.AddDays(-8)
                    },
                    new StoredFile
                    {
                        FileName = "Database_Migration_Snapshot_v2.bak",
                        ContentType = "application/octet-stream",
                        FileSizeBytes = 28500000,
                        StorageProvider = "Azure Blob",
                        Category = "Backups",
                        Description = "Automated nightly schema snapshot and WAL archive checkpoint.",
                        StoragePath = "azure://backups/Database_Migration_Snapshot_v2.bak",
                        UploadedBy = "sysops@platform.local",
                        CreatedAt = DateTime.UtcNow.AddDays(-3)
                    },
                    new StoredFile
                    {
                        FileName = "Enterprise_Master_Service_Agreement.docx",
                        ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                        FileSizeBytes = 820100,
                        StorageProvider = "Local Disk",
                        Category = "Documents",
                        Description = "Standard SLA terms, SLA tiers, and enterprise licensing contracts.",
                        StoragePath = "uploads/documents/Enterprise_Master_Service_Agreement.docx",
                        UploadedBy = "legal@platform.local",
                        CreatedAt = DateTime.UtcNow.AddDays(-18)
                    },
                    new StoredFile
                    {
                        FileName = "Corporate_Brand_Kit_Vector_Icons.zip",
                        ContentType = "application/zip",
                        FileSizeBytes = 12400000,
                        StorageProvider = "AWS S3",
                        Category = "Media",
                        Description = "Vector SVG glyphs, color palettes, and presentation deck templates.",
                        StoragePath = "s3://platform-assets/media/Corporate_Brand_Kit_Vector_Icons.zip",
                        UploadedBy = "design@platform.local",
                        CreatedAt = DateTime.UtcNow.AddDays(-5)
                    }
                );
                await db.SaveChangesAsync();
            }

            // 8. Enterprise Notifications Seed
            if (!await db.Notifications.AnyAsync())
            {
                db.Notifications.AddRange(
                    new AppNotification
                    {
                        Title = "Scheduled Database Maintenance",
                        Message = "Maintenance window tonight from 02:00 to 02:30 UTC for database re-indexing and performance tuning.",
                        Severity = "Warning",
                        TargetAudience = "All Users",
                        IsRead = false,
                        ActionUrl = "/admin/health",
                        CreatedBy = "Site Reliability Engineering",
                        CreatedAt = DateTime.UtcNow.AddHours(-1)
                    },
                    new AppNotification
                    {
                        Title = "DeepSeek-R1 & Gemma AI Node Online",
                        Message = "Enterprise AI Agent Workbench and Vector RAG pipeline have synced 24 fresh knowledge chunks.",
                        Severity = "Success",
                        TargetAudience = "All Users",
                        IsRead = false,
                        ActionUrl = "/ai/agent",
                        CreatedBy = "AI Ops Agent",
                        CreatedAt = DateTime.UtcNow.AddHours(-4)
                    },
                    new AppNotification
                    {
                        Title = "Quarterly SOC2 Security Audit Passed",
                        Message = "Automated compliance auditor reported 0 high-risk vulnerabilities and 100% encryption verification.",
                        Severity = "Info",
                        TargetAudience = "Administrators",
                        IsRead = true,
                        ActionUrl = "/admin/audit-logs",
                        CreatedBy = "Security Compliance Daemon",
                        CreatedAt = DateTime.UtcNow.AddDays(-2)
                    },
                    new AppNotification
                    {
                        Title = "Tenant Bandwidth Threshold Alert",
                        Message = "Tenant 'Stark Logistics' has consumed 92% of monthly bandwidth quota.",
                        Severity = "Error",
                        TargetAudience = "Administrators",
                        IsRead = false,
                        ActionUrl = "/admin/tenants",
                        CreatedBy = "Tenant Metering Engine",
                        CreatedAt = DateTime.UtcNow.AddHours(-8)
                    }
                );
                await db.SaveChangesAsync();
            }

            // 9. Enterprise Text & Email Templates Seed
            if (!await db.TextTemplates.AnyAsync())
            {
                db.TextTemplates.AddRange(
                    new TextTemplate
                    {
                        Name = "App.Email.WelcomeUser",
                        DisplayName = "New User Welcome & Onboarding",
                        Category = "Email",
                        Culture = "en-US",
                        Subject = "Welcome to {{SiteName}}, {{UserName}}!",
                        Content = "Hello {{UserName}},\n\nWelcome to {{TenantName}}! Your enterprise account has been created. You can access your workspace and dashboards using the link below:\n\n{{WorkspaceUrl}}\n\nBest regards,\nThe {{SiteName}} Team",
                        AvailableTokens = "{{UserName}}, {{TenantName}}, {{SiteName}}, {{WorkspaceUrl}}",
                        LastModifiedAt = DateTime.UtcNow.AddDays(-10)
                    },
                    new TextTemplate
                    {
                        Name = "App.Email.PasswordReset",
                        DisplayName = "Security Password Reset Request",
                        Category = "Email",
                        Culture = "en-US",
                        Subject = "Reset Your Password - {{SiteName}}",
                        Content = "Hello {{UserName}},\n\nA request was received to reset your password. Please click the link below to configure a new credential:\n\n{{ResetLink}}\n\nThis single-use link expires in {{ExpiryMinutes}} minutes.\nIf you did not make this request, please contact IT Security immediately.",
                        AvailableTokens = "{{UserName}}, {{ResetLink}}, {{ExpiryMinutes}}, {{SiteName}}",
                        LastModifiedAt = DateTime.UtcNow.AddDays(-14)
                    },
                    new TextTemplate
                    {
                        Name = "App.Email.TenantActivation",
                        DisplayName = "SaaS Tenant Provisioning Notice",
                        Category = "Email",
                        Culture = "en-US",
                        Subject = "Enterprise Tenant '{{TenantName}}' Is Now Live",
                        Content = "Dear Administrator,\n\nYour tenant instance '{{TenantName}}' has been deployed under edition '{{EditionName}}'.\nDedicated Custom Domain: {{CustomDomain}}\nPrimary Admin: {{AdminEmail}}\n\nYour SLA tier is active with 99.9% uptime guarantee.",
                        AvailableTokens = "{{TenantName}}, {{EditionName}}, {{CustomDomain}}, {{AdminEmail}}",
                        LastModifiedAt = DateTime.UtcNow.AddDays(-20)
                    },
                    new TextTemplate
                    {
                        Name = "App.Security.LoginAlert",
                        DisplayName = "Suspicious Login Security Alert",
                        Category = "Security",
                        Culture = "en-US",
                        Subject = "Security Notice: New Login from {{IpAddress}}",
                        Content = "Security Alert: A new login to account '{{UserName}}' was detected from IP address {{IpAddress}} using browser '{{BrowserInfo}}' at {{TimestampUtc}}.\n\nIf this was not you, lock your credentials immediately.",
                        AvailableTokens = "{{UserName}}, {{IpAddress}}, {{BrowserInfo}}, {{TimestampUtc}}",
                        LastModifiedAt = DateTime.UtcNow.AddDays(-5)
                    }
                );
                await db.SaveChangesAsync();
            }

            // 10. Enterprise Localization Languages & Resource Texts Seed
            if (!await db.Languages.AnyAsync())
            {
                db.Languages.AddRange(
                    new LanguageRecord { CultureCode = "en-US", DisplayName = "English (US)", FlagEmoji = "🇺🇸", IsDefault = true, IsActive = true, TranslationProgressPercent = 100 },
                    new LanguageRecord { CultureCode = "es-ES", DisplayName = "Spanish (Spain)", FlagEmoji = "🇪🇸", IsDefault = false, IsActive = true, TranslationProgressPercent = 96 },
                    new LanguageRecord { CultureCode = "de-DE", DisplayName = "German (Germany)", FlagEmoji = "🇩🇪", IsDefault = false, IsActive = true, TranslationProgressPercent = 92 },
                    new LanguageRecord { CultureCode = "fr-FR", DisplayName = "French (France)", FlagEmoji = "🇫🇷", IsDefault = false, IsActive = true, TranslationProgressPercent = 89 },
                    new LanguageRecord { CultureCode = "ja-JP", DisplayName = "Japanese (Japan)", FlagEmoji = "🇯🇵", IsDefault = false, IsActive = true, TranslationProgressPercent = 85 }
                );
                await db.SaveChangesAsync();
            }

            if (!await db.LocalizationTexts.AnyAsync())
            {
                db.LocalizationTexts.AddRange(
                    new LocalizationResourceText { ResourceKey = "General:AppName", CultureCode = "en-US", Value = "Platform Portal", Category = "General" },
                    new LocalizationResourceText { ResourceKey = "General:AppName", CultureCode = "es-ES", Value = "Portal de Plataforma", Category = "General" },
                    new LocalizationResourceText { ResourceKey = "General:AppName", CultureCode = "de-DE", Value = "Plattform-Portal", Category = "General" },
                    new LocalizationResourceText { ResourceKey = "General:Welcome", CultureCode = "en-US", Value = "Welcome to Enterprise Architecture", Category = "General" },
                    new LocalizationResourceText { ResourceKey = "General:Welcome", CultureCode = "es-ES", Value = "Bienvenido a la Arquitectura Empresarial", Category = "General" },
                    new LocalizationResourceText { ResourceKey = "General:Welcome", CultureCode = "de-DE", Value = "Willkommen in der Unternehmensarchitektur", Category = "General" },
                    new LocalizationResourceText { ResourceKey = "Auth:Login", CultureCode = "en-US", Value = "Sign In", Category = "Authentication" },
                    new LocalizationResourceText { ResourceKey = "Auth:Login", CultureCode = "es-ES", Value = "Iniciar Sesión", Category = "Authentication" },
                    new LocalizationResourceText { ResourceKey = "Auth:Login", CultureCode = "de-DE", Value = "Anmelden", Category = "Authentication" },
                    new LocalizationResourceText { ResourceKey = "Auth:Register", CultureCode = "en-US", Value = "Create Account", Category = "Authentication" },
                    new LocalizationResourceText { ResourceKey = "Auth:Register", CultureCode = "es-ES", Value = "Crear Cuenta", Category = "Authentication" },
                    new LocalizationResourceText { ResourceKey = "Common:Save", CultureCode = "en-US", Value = "Save Changes", Category = "Common" },
                    new LocalizationResourceText { ResourceKey = "Common:Save", CultureCode = "es-ES", Value = "Guardar Cambios", Category = "Common" },
                    new LocalizationResourceText { ResourceKey = "Common:Delete", CultureCode = "en-US", Value = "Delete Record", Category = "Common" },
                    new LocalizationResourceText { ResourceKey = "Common:Cancel", CultureCode = "en-US", Value = "Cancel", Category = "Common" }
                );
                await db.SaveChangesAsync();
            }

            // 11. Enterprise OpenIddict / OAuth2 Client Applications Seed
            if (!await db.OAuthClients.AnyAsync())
            {
                db.OAuthClients.AddRange(
                    new OAuthClientApp
                    {
                        ClientId = "Forge.Web.Frontend",
                        ClientName = "Forge Modern Web (MudBlazor SPA)",
                        ClientType = "Public SPA",
                        AllowedGrantTypes = "authorization_code, refresh_token",
                        AllowedScopes = "openid, profile, email, api",
                        RedirectUris = "http://localhost:5000/authentication/login-callback",
                        PostLogoutRedirectUris = "http://localhost:5000/authentication/logout-callback",
                        ClientSecretMasked = "sk_live_••••••••••••••••",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddMonths(-3)
                    },
                    new OAuthClientApp
                    {
                        ClientId = "Forge.Mobile.App",
                        ClientName = "Enterprise Mobile Client (iOS / Android)",
                        ClientType = "Public SPA",
                        AllowedGrantTypes = "authorization_code, refresh_token",
                        AllowedScopes = "openid, profile, api",
                        RedirectUris = "myapp://oauth/callback",
                        PostLogoutRedirectUris = "myapp://oauth/logout",
                        ClientSecretMasked = "sk_live_••••••••••••••••",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddMonths(-2)
                    },
                    new OAuthClientApp
                    {
                        ClientId = "Forge.External.Integration",
                        ClientName = "Enterprise ERP & SAP Sync Worker",
                        ClientType = "Confidential",
                        AllowedGrantTypes = "client_credentials",
                        AllowedScopes = "api",
                        RedirectUris = "https://erp.acmeglobal.com/oauth/token",
                        PostLogoutRedirectUris = null,
                        ClientSecretMasked = "sk_sec_994a28bb01f3e82c••••",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddMonths(-1)
                    },
                    new OAuthClientApp
                    {
                        ClientId = "Forge.Cli.Tooling",
                        ClientName = "Forge Developer Command Line Interface",
                        ClientType = "Confidential",
                        AllowedGrantTypes = "authorization_code, client_credentials",
                        AllowedScopes = "openid, profile, api",
                        RedirectUris = "http://localhost:7890/callback",
                        PostLogoutRedirectUris = null,
                        ClientSecretMasked = "sk_sec_105fa219803bcde1••••",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-15)
                    }
                );
                await db.SaveChangesAsync();
            }
        }

        // Domain Sample Entities
            if (!await db.CloudResources.AnyAsync())
            {
                logger("  [+] Seeding Aspire Orchestrated Cloud Resources");
                db.CloudResources.AddRange(
                    new CloudResource { Name = "apiservice", ResourceType = "Project", EndpointUrl = "http://localhost:5080", State = "Healthy", ReplicaCount = 2, MemoryMb = 210, CpuPercentage = 1.4 },
                    new CloudResource { Name = "webfrontend", ResourceType = "Project", EndpointUrl = "http://localhost:5081", State = "Healthy", ReplicaCount = 1, MemoryMb = 95, CpuPercentage = 0.8 },
                    new CloudResource { Name = "db-migrator", ResourceType = "Project", EndpointUrl = "worker://db-migrator", State = "Completed", ReplicaCount = 1, MemoryMb = 48, CpuPercentage = 0.0 },
                    new CloudResource { Name = "db-sqlite", ResourceType = "Database", EndpointUrl = "sqlite://app.db", State = "Healthy", ReplicaCount = 1, MemoryMb = 64, CpuPercentage = 0.2 },
                    new CloudResource { Name = "cache-resilience", ResourceType = "Polly Pipeline", EndpointUrl = "memory://resilience-cache", State = "Healthy", ReplicaCount = 1, MemoryMb = 32, CpuPercentage = 0.1 },
                    new CloudResource { Name = "otel-collector", ResourceType = "ServiceDiscovery", EndpointUrl = "http://localhost:4317", State = "Healthy", ReplicaCount = 1, MemoryMb = 120, CpuPercentage = 0.5 }
                );
                await db.SaveChangesAsync();
            }

            if (!await db.DistributedOrders.AnyAsync())
            {
                logger("  [+] Seeding Distributed Cloud Orders & Telemetry");
                db.DistributedOrders.AddRange(
                    new DistributedOrder { OrderNumber = "ORD-ASP-1001", ServiceOrigin = "webfrontend", DestinationService = "apiservice", Status = "Completed", TotalAmount = 450.00m, LatencyMs = 42, TraceId = "4bf92f3577b34da6a3ce929d0e0e4736" },
                    new DistributedOrder { OrderNumber = "ORD-ASP-1002", ServiceOrigin = "webfrontend", DestinationService = "apiservice", Status = "Completed", TotalAmount = 1280.50m, LatencyMs = 68, TraceId = "7a29e4b6c1f0412ea8e9324c0d12e892" },
                    new DistributedOrder { OrderNumber = "ORD-ASP-1003", ServiceOrigin = "apiservice", DestinationService = "db-sqlite", Status = "Completed", TotalAmount = 95.00m, LatencyMs = 15, TraceId = "b148c25390e14a1fa629471f83c07214" },
                    new DistributedOrder { OrderNumber = "ORD-ASP-1004", ServiceOrigin = "webfrontend", DestinationService = "cache-resilience", Status = "Completed", TotalAmount = 320.00m, LatencyMs = 28, TraceId = "d872e3518a9042b589417830cb105649" },
                    new DistributedOrder { OrderNumber = "ORD-ASP-1005", ServiceOrigin = "apiservice", DestinationService = "otel-collector", Status = "Completed", TotalAmount = 875.25m, LatencyMs = 54, TraceId = "f531a78942b04f7ca194872c0d513876" }
                );
                await db.SaveChangesAsync();
            }

            if (!await db.TraceRecords.AnyAsync())
            {
                logger("  [+] Seeding OpenTelemetry Distributed Spans & Waterfall Traces");
                db.TraceRecords.AddRange(
                    new DistributedTraceRecord { TraceId = "4bf92f3577b34da6a3ce929d0e0e4736", SpanId = "00f067aa0ba902b7", ServiceName = "webfrontend", OperationName = "HTTP GET /aspire/telemetry", DurationMs = 42, StatusCode = 200 },
                    new DistributedTraceRecord { TraceId = "4bf92f3577b34da6a3ce929d0e0e4736", SpanId = "5fb397be34d23b0f", ServiceName = "apiservice", OperationName = "HTTP GET /api/aspire/resources", DurationMs = 28, StatusCode = 200 },
                    new DistributedTraceRecord { TraceId = "4bf92f3577b34da6a3ce929d0e0e4736", SpanId = "3a18e24c5b70912d", ServiceName = "db-sqlite", OperationName = "EF Core SELECT FROM CloudResources", DurationMs = 8, StatusCode = 200 },
                    new DistributedTraceRecord { TraceId = "7a29e4b6c1f0412ea8e9324c0d12e892", SpanId = "1a89c34d0e51782b", ServiceName = "webfrontend", OperationName = "HTTP POST /api/aspire/orders", DurationMs = 68, StatusCode = 201 },
                    new DistributedTraceRecord { TraceId = "7a29e4b6c1f0412ea8e9324c0d12e892", SpanId = "9f24e10b78c3501a", ServiceName = "apiservice", OperationName = "Activity: ProcessDistributedOrder", DurationMs = 52, StatusCode = 200 },
                    new DistributedTraceRecord { TraceId = "7a29e4b6c1f0412ea8e9324c0d12e892", SpanId = "6c81a0293b4f571e", ServiceName = "db-sqlite", OperationName = "EF Core INSERT INTO DistributedOrders", DurationMs = 12, StatusCode = 200 }
                );
                await db.SaveChangesAsync();
            }
    }
}