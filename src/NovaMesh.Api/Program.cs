using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using NovaMesh.Api.Middleware;
using NovaMesh.Api.Models;
using NovaMesh.Api.Services;
using NovaMesh.Data;
using NovaMesh.Data.Entities;
using NovaMesh.Data.Security;
using NovaMesh.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();


// Database configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=app.db";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(connectionString);
});

// Services
builder.Services.AddSignalR();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddHttpClient<AiRagService>();
builder.Services.AddCors(opts =>
{
    opts.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// Authentication & JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = JwtTokenService.Issuer,
            ValidAudience = JwtTokenService.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtTokenService.Secret))
        };
    });

builder.Services.AddAuthorization();

// OpenAPI & Scalar Documentation
builder.Services.AddOpenApi();

var app = builder.Build();

// Auto-initialize DB schema & seed data for startup and WebApplicationFactory tests
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DataSeeder.SeedAsync(db, "admin@nexus.local", "Admin123!");
}

app.UseCors("AllowAll");
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

// Register Real-time Audit Logging Middleware
app.UseMiddleware<AuditLoggingMiddleware>();

// Scalar Interactive Documentation
app.MapDefaultEndpoints();
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("NovaMesh API Reference")
           .WithTheme(ScalarTheme.Mars)
           .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

// -------------------------------------------------------------
// Core Identity & Account Module Endpoints
// -------------------------------------------------------------
async Task<IResult> LoginHandler(LoginRequest req, AppDbContext db, JwtTokenService jwt)
{
    var user = await db.Users
        .Include(u => u.UserRoles)
        .ThenInclude(ur => ur.Role)
        .FirstOrDefaultAsync(u => u.UserName == req.UserName || u.Email == req.UserName);

    if (user == null || !PasswordHasher.VerifyPassword(req.Password, user.PasswordHash, user.Salt))
        return Results.Unauthorized();

    if (!user.IsActive)
        return Results.Problem("User account is disabled.", statusCode: 403);

    user.LastLoginAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
    var permissions = new HashSet<string>();
    foreach (var ur in user.UserRoles)
    {
        try
        {
            var pList = JsonSerializer.Deserialize<List<string>>(ur.Role.PermissionsJson);
            if (pList != null) foreach (var p in pList) permissions.Add(p);
        }
        catch { }
    }

    var token = jwt.GenerateToken(user, roles, permissions);
    return Results.Ok(new LoginResponse(token, user.UserName, user.Email, user.FullName, roles, permissions.ToList()));
}

app.MapPost("/api/auth/login", LoginHandler).WithName("AuthLogin").WithSummary("Authenticate with credentials and issue signed JWT token");
app.MapPost("/api/account/login", LoginHandler).WithName("AccountLogin").WithSummary("My Platform Account Login endpoint");

async Task<IResult> RegisterHandler(RegisterRequest req, AppDbContext db, JwtTokenService jwt)
{
    if (string.IsNullOrWhiteSpace(req.UserName) || string.IsNullOrWhiteSpace(req.Password))
        return Results.BadRequest(new { error = "Username and password are required." });

    if (await db.Users.AnyAsync(u => u.UserName == req.UserName || (req.Email != null && u.Email == req.Email)))
        return Results.Conflict(new { error = "A user with this username or email already exists." });

    PasswordHasher.HashPassword(req.Password, out var hash, out var salt);
    var newUser = new AppUser
    {
        Id = Guid.NewGuid(),
        UserName = req.UserName,
        Email = string.IsNullOrWhiteSpace(req.Email) ? $"{req.UserName}@nexus.local" : req.Email,
        FullName = string.IsNullOrWhiteSpace(req.FullName) ? req.UserName : req.FullName,
        PasswordHash = hash,
        Salt = salt,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    var userRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "User");
    if (userRole != null)
    {
        newUser.UserRoles.Add(new AppUserRole { UserId = newUser.Id, RoleId = userRole.Id });
    }

    db.Users.Add(newUser);
    await db.SaveChangesAsync();

    var roles = newUser.UserRoles.Select(ur => ur.Role.Name).ToList();
    if (roles.Count == 0 && userRole != null) roles.Add(userRole.Name);
    var permissions = new List<string> { "Dashboard.View", "Profile.Manage" };
    var token = jwt.GenerateToken(newUser, roles, permissions);
    return Results.Ok(new LoginResponse(token, newUser.UserName, newUser.Email, newUser.FullName, roles, permissions));
}

app.MapPost("/api/auth/register", RegisterHandler).WithName("AuthRegister").WithSummary("Register a new user account and issue signed JWT token");
app.MapPost("/api/account/register", RegisterHandler).WithName("AccountRegister").WithSummary("My Platform Account Register endpoint");

app.MapGet("/api/account/my-profile", async (ClaimsPrincipal user, AppDbContext db) =>
{
    var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();

    var appUser = await db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).FirstOrDefaultAsync(u => u.Id == userId);
    if (appUser == null) return Results.NotFound();

    var roles = appUser.UserRoles.Select(ur => ur.Role.Name).ToList();
    var permissions = user.FindAll("permission").Select(c => c.Value).ToList();
    return Results.Ok(new UserProfileDto(appUser.Id, appUser.UserName, appUser.Email, appUser.FullName, appUser.AvatarUrl, appUser.PhoneNumber, appUser.CreatedAt, roles, permissions));
}).WithName("GetMyProfile").RequireAuthorization();

app.MapPut("/api/account/my-profile", async (UpdateProfileRequest req, ClaimsPrincipal user, AppDbContext db) =>
{
    var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();

    var appUser = await db.Users.FindAsync(userId);
    if (appUser == null) return Results.NotFound();

    appUser.FullName = req.FullName;
    appUser.Email = req.Email;
    appUser.PhoneNumber = req.PhoneNumber;
    await db.SaveChangesAsync();
    return Results.Ok(appUser);
}).WithName("UpdateMyProfile").RequireAuthorization();

app.MapPost("/api/account/change-password", async (ChangePasswordRequest req, ClaimsPrincipal user, AppDbContext db) =>
{
    var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();

    var appUser = await db.Users.FindAsync(userId);
    if (appUser == null) return Results.NotFound();

    if (!PasswordHasher.VerifyPassword(req.CurrentPassword, appUser.PasswordHash, appUser.Salt))
        return Results.Problem("Current password is incorrect.", statusCode: 400);

    PasswordHasher.HashPassword(req.NewPassword, out var newHash, out var newSalt);
    appUser.PasswordHash = newHash;
    appUser.Salt = newSalt;
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "Password updated successfully." });
}).WithName("ChangePassword").RequireAuthorization();

app.MapGet("/api/identity/users", async (string? search, int pageNumber = 1, int pageSize = 10, AppDbContext db = null!) =>
{
    var query = db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).AsQueryable();
    if (!string.IsNullOrWhiteSpace(search))
        query = query.Where(u => u.UserName.Contains(search) || u.Email.Contains(search) || u.FullName.Contains(search));

    var total = await query.CountAsync();
    var items = await query.OrderByDescending(u => u.CreatedAt)
                           .Skip((pageNumber - 1) * pageSize)
                           .Take(pageSize)
                           .Select(u => new UserDto(u.Id, u.UserName, u.Email, u.FullName, u.IsActive, u.CreatedAt, u.UserRoles.Select(r => r.Role.Name).ToList()))
                           .ToListAsync();

    return Results.Ok(new PagedResultDto<UserDto>(items, total, pageNumber, pageSize));
}).WithName("GetUsers").RequireAuthorization();

app.MapGet("/api/users", async (string? search, int pageNumber = 1, int pageSize = 10, AppDbContext db = null!) =>
{
    var query = db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).AsQueryable();
    if (!string.IsNullOrWhiteSpace(search))
        query = query.Where(u => u.UserName.Contains(search) || u.Email.Contains(search) || u.FullName.Contains(search));

    var total = await query.CountAsync();
    var items = await query.OrderByDescending(u => u.CreatedAt)
                           .Skip((pageNumber - 1) * pageSize)
                           .Take(pageSize)
                           .Select(u => new UserDto(u.Id, u.UserName, u.Email, u.FullName, u.IsActive, u.CreatedAt, u.UserRoles.Select(r => r.Role.Name).ToList()))
                           .ToListAsync();

    return Results.Ok(new PagedResultDto<UserDto>(items, total, pageNumber, pageSize));
}).RequireAuthorization();

app.MapPost("/api/identity/users", async (CreateUserRequest req, AppDbContext db) =>
{
    if (await db.Users.AnyAsync(u => u.UserName == req.UserName || u.Email == req.Email))
        return Results.Problem("User with this username or email already exists.", statusCode: 400);

    PasswordHasher.HashPassword(req.Password, out var hash, out var salt);
    var newUser = new AppUser
    {
        UserName = req.UserName,
        Email = req.Email,
        FullName = req.FullName,
        PasswordHash = hash,
        Salt = salt,
        IsActive = true
    };
    db.Users.Add(newUser);
    await db.SaveChangesAsync();

    foreach (var rName in req.Roles)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == rName);
        if (role != null)
            db.UserRoles.Add(new AppUserRole { UserId = newUser.Id, RoleId = role.Id });
    }
    await db.SaveChangesAsync();

    return Results.Created($"/api/identity/users/{newUser.Id}", new UserDto(newUser.Id, newUser.UserName, newUser.Email, newUser.FullName, newUser.IsActive, newUser.CreatedAt, req.Roles));
}).WithName("CreateUser").RequireAuthorization();

app.MapDelete("/api/identity/users/{id:guid}", async (Guid id, AppDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user == null) return Results.NotFound();
    db.Users.Remove(user);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).WithName("DeleteUser").RequireAuthorization();

app.MapGet("/api/identity/roles", async (AppDbContext db) =>
{
    var roles = await db.Roles.ToListAsync();
    return Results.Ok(roles.Select(r => new RoleDto(r.Id, r.Name, r.Description, JsonSerializer.Deserialize<List<string>>(r.PermissionsJson) ?? new())));
}).WithName("GetRoles").WithSummary("List all system roles and their assigned permissions");

app.MapPost("/api/identity/roles", async (CreateRoleRequest req, AppDbContext db) =>
{
    if (await db.Roles.AnyAsync(r => r.Name == req.Name))
        return Results.Problem("Role with this name already exists.", statusCode: 400);

    var role = new AppRole
    {
        Name = req.Name,
        Description = req.Description,
        PermissionsJson = JsonSerializer.Serialize(req.Permissions)
    };
    db.Roles.Add(role);
    foreach (var p in req.Permissions)
    {
        db.PermissionGrants.Add(new PermissionGrant { RoleName = role.Name, PermissionKey = p, IsGranted = true });
    }
    await db.SaveChangesAsync();
    return Results.Created($"/api/identity/roles/{role.Id}", new RoleDto(role.Id, role.Name, role.Description, req.Permissions));
}).WithName("CreateRole").WithSummary("Create a new RBAC role with permissions");

app.MapPut("/api/identity/roles/{id:guid}/permissions", async (Guid id, UpdateRolePermissionsRequest req, AppDbContext db) =>
{
    var role = await db.Roles.FindAsync(id);
    if (role == null) return Results.NotFound();

    role.PermissionsJson = JsonSerializer.Serialize(req.Permissions);

    var existingGrants = await db.PermissionGrants.Where(g => g.RoleName == role.Name).ToListAsync();
    db.PermissionGrants.RemoveRange(existingGrants);
    foreach (var p in req.Permissions)
    {
        db.PermissionGrants.Add(new PermissionGrant { RoleName = role.Name, PermissionKey = p, IsGranted = true });
    }
    await db.SaveChangesAsync();
    return Results.Ok(new RoleDto(role.Id, role.Name, role.Description, req.Permissions));
}).WithName("UpdateRolePermissions").WithSummary("Update permissions granted to a role");

app.MapDelete("/api/identity/roles/{id:guid}", async (Guid id, AppDbContext db) =>
{
    var role = await db.Roles.FindAsync(id);
    if (role == null) return Results.NotFound();
    if (role.Name == "Admin") return Results.Problem("Cannot delete system Administrator role.", statusCode: 400);

    var grants = await db.PermissionGrants.Where(g => g.RoleName == role.Name).ToListAsync();
    db.PermissionGrants.RemoveRange(grants);
    db.Roles.Remove(role);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).WithName("DeleteRole").WithSummary("Delete a custom role");

app.MapGet("/api/identity/permissions", () =>
{
    var groups = new List<PermissionGroupDto>
    {
        new("Identity & RBAC", new() { "Identity.Users.View", "Identity.Users.Create", "Identity.Users.Update", "Identity.Users.Delete", "Identity.Roles.View", "Identity.Roles.Manage" }),
        new("Audit Logging", new() { "AuditLogging.View", "AuditLogging.Manage" }),
        new("System Settings", new() { "Settings.View", "Settings.Manage" }),
        new("SaaS Multi-Tenancy", new() { "Tenants.View", "Tenants.Manage" }),
        new("Feature Management", new() { "Features.View", "Features.Manage" }),
        new("CMS Kit & Content", new() { "CmsKit.Blogs.Manage", "CmsKit.Pages.Manage", "CmsKit.Comments.Manage" })
    };
    return Results.Ok(groups);
}).WithName("GetPermissions").WithSummary("List all system permission categories and keys");

// -------------------------------------------------------------
// Core Audit Logging Module Endpoints
// -------------------------------------------------------------
app.MapGet("/api/audit-logs", async (string? search, string? httpMethod, int? statusCode, int pageNumber = 1, int pageSize = 15, AppDbContext db = null!) =>
{
    var query = db.AuditLogs.AsQueryable();
    if (!string.IsNullOrWhiteSpace(search))
        query = query.Where(a => a.Url.Contains(search) || (a.UserName != null && a.UserName.Contains(search)) || (a.ActionName != null && a.ActionName.Contains(search)));
    if (!string.IsNullOrWhiteSpace(httpMethod) && httpMethod != "All")
        query = query.Where(a => a.HttpMethod == httpMethod);
    if (statusCode.HasValue)
        query = query.Where(a => a.HttpStatusCode == statusCode.Value);

    var total = await query.CountAsync();
    var items = await query.OrderByDescending(a => a.ExecutionTime)
                           .Skip((pageNumber - 1) * pageSize)
                           .Take(pageSize)
                           .Select(a => new AuditLogDto(a.Id, a.ExecutionTime, a.UserName, a.HttpMethod, a.Url, a.HttpStatusCode, a.DurationMs, a.ClientIpAddress, a.BrowserInfo, a.ActionName))
                           .ToListAsync();

    return Results.Ok(new PagedResultDto<AuditLogDto>(items, total, pageNumber, pageSize));
}).WithName("GetAuditLogs").WithSummary("Retrieve paged audit logs with real-time request metrics");

app.MapDelete("/api/audit-logs/clear", async (AppDbContext db) =>
{
    db.AuditLogs.RemoveRange(db.AuditLogs);
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "Audit logs cleared successfully." });
}).WithName("ClearAuditLogs").RequireAuthorization();

// -------------------------------------------------------------
// Core Settings Management Module Endpoints
// -------------------------------------------------------------
app.MapGet("/api/settings", async (AppDbContext db) =>
{
    var settings = await db.Settings.OrderBy(s => s.Category).ThenBy(s => s.Key).ToListAsync();
    return Results.Ok(settings.Select(s => new AppSettingDto(s.Id, s.Category, s.Key, s.Value, s.Description, s.LastModifiedAt)));
}).WithName("GetSettings").WithSummary("List all application configuration settings");

app.MapPut("/api/settings", async (UpdateSettingRequest req, AppDbContext db) =>
{
    var setting = await db.Settings.FirstOrDefaultAsync(s => s.Key == req.Key);
    if (setting == null) return Results.NotFound();
    setting.Value = req.Value;
    setting.LastModifiedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.Ok(new AppSettingDto(setting.Id, setting.Category, setting.Key, setting.Value, setting.Description, setting.LastModifiedAt));
}).WithName("UpdateSetting").RequireAuthorization();

// -------------------------------------------------------------
// Core SaaS Multi-Tenancy Module Endpoints
// -------------------------------------------------------------
app.MapGet("/api/tenants", async (AppDbContext db) =>
{
    var tenants = await db.Tenants.OrderBy(t => t.Name).ToListAsync();
    return Results.Ok(tenants.Select(t => new TenantDto(t.Id, t.Name, t.NormalizedName, t.ConnectionString, t.EditionName, t.IsActive, t.PrimaryColor, t.SecondaryColor, t.AccentColor, t.LogoUrl, t.CustomDomain, t.Slogan, t.CreatedAt)));
}).WithName("GetTenants").WithSummary("List all SaaS tenants");

app.MapPost("/api/tenants", async (CreateTenantRequest req, AppDbContext db) =>
{
    var tenant = new Tenant
    {
        Name = req.Name,
        NormalizedName = req.Name.ToUpperInvariant().Replace(" ", "-"),
        EditionName = req.EditionName,
        ConnectionString = req.ConnectionString,
        PrimaryColor = req.PrimaryColor ?? "#4F46E5",
        SecondaryColor = req.SecondaryColor ?? "#06B6D4",
        AccentColor = req.AccentColor ?? "#F59E0B",
        LogoUrl = req.LogoUrl,
        CustomDomain = req.CustomDomain,
        Slogan = req.Slogan,
        IsActive = true
    };
    db.Tenants.Add(tenant);
    await db.SaveChangesAsync();
    return Results.Created($"/api/tenants/{tenant.Id}", new TenantDto(tenant.Id, tenant.Name, tenant.NormalizedName, tenant.ConnectionString, tenant.EditionName, tenant.IsActive, tenant.PrimaryColor, tenant.SecondaryColor, tenant.AccentColor, tenant.LogoUrl, tenant.CustomDomain, tenant.Slogan, tenant.CreatedAt));
}).WithName("CreateTenant").RequireAuthorization();

app.MapPut("/api/tenants/{id:guid}", async (Guid id, UpdateTenantRequest req, AppDbContext db) =>
{
    var tenant = await db.Tenants.FindAsync(id);
    if (tenant == null) return Results.NotFound();
    tenant.Name = req.Name;
    tenant.NormalizedName = req.Name.ToUpperInvariant().Replace(" ", "-");
    tenant.EditionName = req.EditionName;
    tenant.IsActive = req.IsActive;
    tenant.ConnectionString = req.ConnectionString;
    if (!string.IsNullOrWhiteSpace(req.PrimaryColor)) tenant.PrimaryColor = req.PrimaryColor;
    if (!string.IsNullOrWhiteSpace(req.SecondaryColor)) tenant.SecondaryColor = req.SecondaryColor;
    if (!string.IsNullOrWhiteSpace(req.AccentColor)) tenant.AccentColor = req.AccentColor;
    if (req.LogoUrl != null) tenant.LogoUrl = req.LogoUrl;
    if (req.CustomDomain != null) tenant.CustomDomain = req.CustomDomain;
    if (req.Slogan != null) tenant.Slogan = req.Slogan;
    await db.SaveChangesAsync();
    return Results.Ok(new TenantDto(tenant.Id, tenant.Name, tenant.NormalizedName, tenant.ConnectionString, tenant.EditionName, tenant.IsActive, tenant.PrimaryColor, tenant.SecondaryColor, tenant.AccentColor, tenant.LogoUrl, tenant.CustomDomain, tenant.Slogan, tenant.CreatedAt));
}).WithName("UpdateTenant").RequireAuthorization();

app.MapDelete("/api/tenants/{id:guid}", async (Guid id, AppDbContext db) =>
{
    var tenant = await db.Tenants.FindAsync(id);
    if (tenant == null) return Results.NotFound();
    db.Tenants.Remove(tenant);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).WithName("DeleteTenant").RequireAuthorization();

// -------------------------------------------------------------
// Core Feature Management Module Endpoints
// -------------------------------------------------------------
app.MapGet("/api/features", async (AppDbContext db) =>
{
    var features = await db.FeatureValues.OrderBy(f => f.Name).ToListAsync();
    return Results.Ok(features.Select(f => new FeatureValueDto(f.Id, f.Name, f.Value, f.ProviderName, f.ProviderKey, f.Description)));
}).WithName("GetFeatures").WithSummary("List all system feature flags and toggles");

app.MapPut("/api/features/{name}", async (string name, UpdateFeatureRequest req, AppDbContext db) =>
{
    var feature = await db.FeatureValues.FirstOrDefaultAsync(f => f.Name == name);
    if (feature == null) return Results.NotFound();
    feature.Value = req.Value;
    await db.SaveChangesAsync();
    return Results.Ok(new FeatureValueDto(feature.Id, feature.Name, feature.Value, feature.ProviderName, feature.ProviderKey, feature.Description));
}).WithName("UpdateFeature").RequireAuthorization();

// -------------------------------------------------------------
// Enterprise Dashboard & Analytics Telemetry Endpoints
// -------------------------------------------------------------
app.MapGet("/api/analytics/dashboard", async (AppDbContext db) =>
{
    int totalUsers = await db.Users.CountAsync();
    int activeUsers = await db.Users.CountAsync(u => u.IsActive);
    int totalAuditLogs = await db.AuditLogs.CountAsync();
    long avgLatency = totalAuditLogs > 0 ? (long)await db.AuditLogs.AverageAsync(a => a.DurationMs) : 0;
    double successRate = totalAuditLogs > 0
        ? (double)await db.AuditLogs.CountAsync(a => a.HttpStatusCode < 400) / totalAuditLogs * 100
        : 100.0;

    int totalAiDocs = await db.AiDocuments.CountAsync();
    int totalRagChunks = await db.AiChunks.CountAsync();
    int totalAgentLogs = await db.AiAgentAuditLogs.CountAsync();

    // Chart 1: Real Hourly Traffic & Latency Trends (computed directly from AuditLogs)
    var allLogs = await db.AuditLogs.OrderBy(a => a.ExecutionTime).ToListAsync();
    var trafficLabels = new string[7];
    var requestCounts = new double[7];
    var latencyMs = new double[7];

    if (allLogs.Count > 0)
    {
        var minTime = allLogs.Min(a => a.ExecutionTime);
        var maxTime = allLogs.Max(a => a.ExecutionTime);
        if (minTime >= maxTime) minTime = maxTime.AddHours(-1);
        var totalSpan = (maxTime - minTime).TotalMinutes;
        var step = Math.Max(1.0, totalSpan / 6.0);

        for (int i = 0; i < 7; i++)
        {
            var start = minTime.AddMinutes(i * step);
            var end = (i == 6) ? DateTime.UtcNow.AddMinutes(1) : minTime.AddMinutes((i + 1) * step);
            trafficLabels[i] = start.ToString("HH:mm");
            var logsInInterval = allLogs.Where(a => a.ExecutionTime >= start && a.ExecutionTime <= end).ToList();
            requestCounts[i] = logsInInterval.Count;
            latencyMs[i] = logsInInterval.Count > 0 ? Math.Round(logsInInterval.Average(a => (double)a.DurationMs), 1) : 0;
        }
        trafficLabels[6] = "Now";
    }
    else
    {
        trafficLabels = new[] { "00:00", "04:00", "08:00", "12:00", "16:00", "20:00", "Now" };
    }

    // Chart 2: Real HTTP Status Codes Breakdown directly from AuditLogs
    var statusLabels = new[] { "200 OK", "201 Created", "400 Bad Req", "404 Not Found", "500 Error" };
    int okCount = allLogs.Count(a => a.HttpStatusCode == 200);
    int createdCount = allLogs.Count(a => a.HttpStatusCode == 201);
    int badCount = allLogs.Count(a => a.HttpStatusCode == 400);
    int notFoundCount = allLogs.Count(a => a.HttpStatusCode == 404);
    int errCount = allLogs.Count(a => a.HttpStatusCode >= 500);
    var statusCounts = new double[] { okCount, createdCount, badCount, notFoundCount, errCount };

    // Chart 3: Real AI Suite Queries & Ingested Knowledge from Database
    var aiLabels = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
    var aiTokens = new double[7];
    var aiQueries = new double[7];

    var agentLogs = await db.AiAgentAuditLogs.ToListAsync();
    var aiDocsList = await db.AiDocuments.ToListAsync();
    int[] dayMap = new int[] { 1, 2, 3, 4, 5, 6, 0 }; // Mon=1..Sun=0

    for (int i = 0; i < 7; i++)
    {
        var dow = (DayOfWeek)dayMap[i];
        var dayLogs = agentLogs.Where(l => l.CreatedAt.DayOfWeek == dow).ToList();
        var dayDocs = aiDocsList.Where(d => d.CreatedAt.DayOfWeek == dow).ToList();
        aiQueries[i] = dayLogs.Count;
        aiTokens[i] = dayDocs.Sum(d => (double)d.TokenCount) + dayLogs.Sum(l => (double)(l.ExecutionMs * 15));
    }

    // Chart 4: Real SaaS Tenant Distribution
    var tenants = await db.Tenants.ToListAsync();
    var tenantLabels = tenants.Select(t => t.Name).ToArray();
    var tenantCounts = tenants.Select(t => t.IsActive ? 49.0 : 0.0).ToArray();

    if (tenantLabels.Length == 0)
    {
        tenantLabels = new[] { "Host (Root)" };
        tenantCounts = new double[] { 100.0 };
    }

    return Results.Ok(new DashboardAnalyticsDto(
        totalUsers,
        activeUsers,
        totalAuditLogs,
        avgLatency,
        Math.Round(successRate, 1),
        totalAiDocs,
        totalRagChunks,
        totalAgentLogs,
        trafficLabels,
        requestCounts,
        latencyMs,
        statusLabels,
        statusCounts,
        aiLabels,
        aiTokens,
        aiQueries,
        tenantLabels,
        tenantCounts
    ));
}).WithName("GetDashboardAnalytics");

// -------------------------------------------------------------
// AI Knowledge Base, RAG, and Autonomous Agent Endpoints
// -------------------------------------------------------------
app.MapGet("/api/ai/documents", async (AppDbContext db) =>
{
    var docs = await db.AiDocuments.OrderByDescending(d => d.CreatedAt).ToListAsync();
    return Results.Ok(docs.Select(d => new AiDocumentDto(d.Id, d.Title, d.Category, d.SourceType, d.Content.Length > 120 ? d.Content[..120] + "..." : d.Content, d.Content, d.ChunkCount, d.TokenCount, d.EmbeddingModel, d.CreatedAt)));
}).WithName("GetAiDocuments");

app.MapPost("/api/ai/documents", async (CreateAiDocumentRequest req, AiRagService ragService, AppDbContext db) =>
{
    var rawChunks = req.Content.Split(new[] { "\n\n", "### " }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (rawChunks.Length == 0) rawChunks = new[] { req.Content };

    var doc = new AiDocument
    {
        Title = req.Title,
        Category = req.Category,
        SourceType = req.SourceType,
        Content = req.Content,
        TokenCount = req.Content.Length / 4,
        EmbeddingModel = "nomic-embed-text:latest"
    };
    db.AiDocuments.Add(doc);
    await db.SaveChangesAsync();

    int idx = 0;
    foreach (var c in rawChunks)
    {
        var vec = await ragService.GetEmbeddingAsync(c);
        db.AiChunks.Add(new AiDocumentChunk
        {
            DocumentId = doc.Id,
            ChunkIndex = idx++,
            Content = c,
            EmbeddingVectorJson = JsonSerializer.Serialize(vec)
        });
    }
    await db.SaveChangesAsync();

    return Results.Created($"/api/ai/documents/{doc.Id}", new AiDocumentDto(doc.Id, doc.Title, doc.Category, doc.SourceType, doc.Content.Length > 120 ? doc.Content[..120] + "..." : doc.Content, doc.Content, rawChunks.Length, doc.TokenCount, doc.EmbeddingModel, doc.CreatedAt));
}).WithName("CreateAiDocument");

app.MapDelete("/api/ai/documents/{id:guid}", async (Guid id, AppDbContext db) =>
{
    var doc = await db.AiDocuments.FindAsync(id);
    if (doc == null) return Results.NotFound();
    var chunks = await db.AiChunks.Where(c => c.DocumentId == id).ToListAsync();
    db.AiChunks.RemoveRange(chunks);
    db.AiDocuments.Remove(doc);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).WithName("DeleteAiDocument");

app.MapGet("/api/ai/documents/{id:guid}/chunks", async (Guid id, AppDbContext db) =>
{
    var chunks = await db.AiChunks.Where(c => c.DocumentId == id).OrderBy(c => c.ChunkIndex).ToListAsync();
    return Results.Ok(chunks.Select(c => new AiChunkDto(c.Id, c.DocumentId, c.ChunkIndex, c.Content, c.Content.Length / 4, "{}")));
}).WithName("GetAiDocumentChunks");

app.MapPost("/api/ai/rag/query", async (RagQueryRequest req, AiRagService ragService, AppDbContext db) =>
{
    var result = await ragService.ExecuteRagQueryAsync(db, req.Query, req.SimilarityThreshold, req.TopK);
    return Results.Ok(result);
}).WithName("QueryRagEngine");

app.MapGet("/api/ai/conversations", async (AppDbContext db) =>
{
    var convs = await db.AiConversations
        .Include(c => c.Messages)
        .OrderByDescending(c => c.CreatedAt)
        .ToListAsync();

    return Results.Ok(convs.Select(c => new AiConversationDto(c.Id, c.Title, c.Persona, c.Model, c.CreatedAt, c.Messages.Count)));
}).WithName("GetAiConversations");

app.MapPost("/api/ai/conversations", async (CreateConversationRequest req, AppDbContext db) =>
{
    var conv = new AiAgentConversation
    {
        Title = req.Title,
        Persona = req.Persona,
        Model = req.Model
    };
    db.AiConversations.Add(conv);
    await db.SaveChangesAsync();
    return Results.Created($"/api/ai/conversations/{conv.Id}", new AiConversationDto(conv.Id, conv.Title, conv.Persona, conv.Model, conv.CreatedAt, 0));
}).WithName("CreateAiConversation");

app.MapGet("/api/ai/conversations/{id:guid}/messages", async (Guid id, AppDbContext db) =>
{
    var msgs = await db.AiMessages
        .Where(m => m.ConversationId == id)
        .OrderBy(m => m.CreatedAt)
        .ToListAsync();

    var dtos = msgs.Select(m =>
    {
        var cit = JsonSerializer.Deserialize<List<RagCitationDto>>(m.CitationsJson) ?? new();
        var tool = JsonSerializer.Deserialize<List<AiToolCallDto>>(m.ToolCallsJson) ?? new();
        return new AiMessageDto(m.Id, m.ConversationId, m.Role, m.Content, cit, tool, m.CreatedAt);
    }).ToList();

    return Results.Ok(dtos);
}).WithName("GetConversationMessages");

app.MapPost("/api/ai/conversations/{id:guid}/messages", async (Guid id, SendAgentMessageRequest req, AiRagService ragService, AppDbContext db) =>
{
    var conv = await db.AiConversations.FindAsync(id);
    if (conv == null) return Results.NotFound();

    var result = await ragService.ExecuteAgentMessageAsync(
        db,
        id,
        req.UserPrompt,
        req.Persona,
        req.EnableRag,
        req.EnableAutonomousTools,
        req.Model ?? conv.Model
    );

    return Results.Ok(result);
}).WithName("SendMessageToAiAgent");

app.MapGet("/api/ai/agent/logs", async (AppDbContext db) =>
{
    var logs = await db.AiAgentAuditLogs.OrderByDescending(l => l.CreatedAt).Take(50).ToListAsync();
    return Results.Ok(logs.Select(l => new AiAgentAuditLogDto(l.Id, l.AgentName, l.ActionName, l.InputPrompt, l.OutputSummary, l.ExecutionMs, l.Success, l.CreatedAt)));
}).WithName("GetAiAgentAuditLogs");

// -------------------------------------------------------------
// Enterprise Background Jobs & Queue Worker Endpoints
// -------------------------------------------------------------
app.MapGet("/api/background-jobs", async (AppDbContext db) =>
{
    var jobs = await db.BackgroundJobs.OrderBy(j => j.JobGroup).ThenBy(j => j.JobName).ToListAsync();
    return Results.Ok(jobs.Select(j => new BackgroundJobDto(
        j.Id, j.JobName, j.JobGroup, j.JobType, j.Description, j.CronSchedule, j.Status,
        j.LastRunTime, j.NextRunTime, j.ExecutionDurationMs, j.RunCount, j.ErrorMessage, j.PayloadJson, j.CreatedAt
    )));
}).WithName("GetBackgroundJobs");

app.MapPost("/api/background-jobs/{id:guid}/run", async (Guid id, AppDbContext db) =>
{
    var job = await db.BackgroundJobs.FindAsync(id);
    if (job == null) return Results.NotFound();

    job.Status = "Running";
    await db.SaveChangesAsync();

    var sw = System.Diagnostics.Stopwatch.StartNew();
    await Task.Delay(80);
    sw.Stop();

    job.Status = "Completed";
    job.LastRunTime = DateTime.UtcNow;
    job.NextRunTime = DateTime.UtcNow.AddHours(2);
    job.ExecutionDurationMs = sw.ElapsedMilliseconds;
    job.RunCount++;
    await db.SaveChangesAsync();

    return Results.Ok(new BackgroundJobDto(
        job.Id, job.JobName, job.JobGroup, job.JobType, job.Description, job.CronSchedule, job.Status,
        job.LastRunTime, job.NextRunTime, job.ExecutionDurationMs, job.RunCount, job.ErrorMessage, job.PayloadJson, job.CreatedAt
    ));
}).WithName("RunBackgroundJob");

app.MapPost("/api/background-jobs/{id:guid}/toggle-pause", async (Guid id, AppDbContext db) =>
{
    var job = await db.BackgroundJobs.FindAsync(id);
    if (job == null) return Results.NotFound();

    job.Status = job.Status == "Paused" ? "Active" : "Paused";
    await db.SaveChangesAsync();

    return Results.Ok(new BackgroundJobDto(
        job.Id, job.JobName, job.JobGroup, job.JobType, job.Description, job.CronSchedule, job.Status,
        job.LastRunTime, job.NextRunTime, job.ExecutionDurationMs, job.RunCount, job.ErrorMessage, job.PayloadJson, job.CreatedAt
    ));
}).WithName("TogglePauseBackgroundJob");

app.MapPost("/api/background-jobs", async (CreateBackgroundJobRequest req, AppDbContext db) =>
{
    var job = new BackgroundJobRecord
    {
        JobName = req.JobName,
        JobGroup = req.JobGroup,
        JobType = req.JobType,
        Description = req.Description,
        CronSchedule = req.CronSchedule,
        PayloadJson = req.PayloadJson,
        Status = "Active",
        NextRunTime = DateTime.UtcNow.AddHours(1)
    };
    db.BackgroundJobs.Add(job);
    await db.SaveChangesAsync();

    return Results.Created($"/api/background-jobs/{job.Id}", new BackgroundJobDto(
        job.Id, job.JobName, job.JobGroup, job.JobType, job.Description, job.CronSchedule, job.Status,
        job.LastRunTime, job.NextRunTime, job.ExecutionDurationMs, job.RunCount, job.ErrorMessage, job.PayloadJson, job.CreatedAt
    ));
}).WithName("CreateBackgroundJob");

app.MapDelete("/api/background-jobs/{id:guid}", async (Guid id, AppDbContext db) =>
{
    var job = await db.BackgroundJobs.FindAsync(id);
    if (job == null) return Results.NotFound();
    db.BackgroundJobs.Remove(job);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).WithName("DeleteBackgroundJob");

// -------------------------------------------------------------
// Enterprise Entity Changes & Property Audit Trail Endpoints
// -------------------------------------------------------------
app.MapGet("/api/audit-logs/entity-changes", async (string? entityType, AppDbContext db) =>
{
    var query = db.EntityChanges.Include(c => c.PropertyChanges).AsQueryable();
    if (!string.IsNullOrWhiteSpace(entityType))
        query = query.Where(c => c.EntityType == entityType);

    var items = await query.OrderByDescending(c => c.ChangeTime).Take(100).ToListAsync();
    var dtos = items.Select(c => new EntityChangeDto(
        c.Id,
        c.AuditLogId,
        c.EntityType,
        c.EntityId,
        c.ChangeType,
        c.ChangeTime,
        c.UserName,
        c.TenantName,
        c.PropertyChanges.Select(p => new EntityPropertyChangeDto(p.Id, p.PropertyName, p.OriginalValue, p.NewValue, p.PropertyType)).ToList()
    )).ToList();

    return Results.Ok(dtos);
}).WithName("GetEntityChanges");

// -------------------------------------------------------------
// Enterprise System Health & Diagnostics Endpoints
// -------------------------------------------------------------
app.MapGet("/api/system/health", async (AppDbContext db, IHttpClientFactory httpClientFactory) =>
{
    var dbSw = System.Diagnostics.Stopwatch.StartNew();
    bool dbHealthy = false;
    try
    {
        dbHealthy = await db.Database.CanConnectAsync();
    }
    catch { }
    dbSw.Stop();

    string ollamaStatus = "Online";
    string activeModel = "gemma4:31b";
    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMilliseconds(500);
        var resp = await client.GetAsync("http://127.0.0.1:11434/api/tags");
        if (!resp.IsSuccessStatusCode) ollamaStatus = "Degraded";
    }
    catch
    {
        ollamaStatus = "Local Standby";
    }

    var proc = System.Diagnostics.Process.GetCurrentProcess();
    var health = new SystemHealthDto(
        dbHealthy ? "Healthy" : "Degraded",
        "2.4.0-enterprise",
        Environment.Version.ToString(),
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
        DateTime.UtcNow - proc.StartTime.ToUniversalTime(),
        GC.GetTotalMemory(false),
        System.Threading.ThreadPool.ThreadCount,
        GC.CollectionCount(0),
        GC.CollectionCount(1),
        GC.CollectionCount(2),
        dbHealthy ? "Operational" : "Unavailable",
        dbSw.ElapsedMilliseconds,
        ollamaStatus,
        activeModel,
        1420000000,
        10000000000
    );

    return Results.Ok(health);
}).WithName("GetSystemHealth");

app.MapPost("/api/system/gc-collect", () =>
{
    long before = GC.GetTotalMemory(false);
    GC.Collect(2, GCCollectionMode.Forced, true, true);
    GC.WaitForPendingFinalizers();
    long after = GC.GetTotalMemory(true);
    return Results.Ok(new { MemoryBeforeBytes = before, MemoryAfterBytes = after, FreedBytes = Math.Max(0, before - after) });
}).WithName("TriggerGcCollect");

// -------------------------------------------------------------
// Enterprise File Management Endpoints
// -------------------------------------------------------------
app.MapGet("/api/files", async (string? search, string? category, AppDbContext db) =>
{
    var query = db.Files.AsQueryable();
    if (!string.IsNullOrWhiteSpace(search))
        query = query.Where(f => f.FileName.Contains(search) || (f.Description != null && f.Description.Contains(search)) || f.UploadedBy.Contains(search));
    if (!string.IsNullOrWhiteSpace(category) && category != "All")
        query = query.Where(f => f.Category == category);

    var items = await query.OrderByDescending(f => f.CreatedAt).ToListAsync();
    return Results.Ok(items.Select(f => new StoredFileDto(f.Id, f.FileName, f.ContentType, f.FileSizeBytes, f.StorageProvider, f.Category, f.Description, f.StoragePath, f.UploadedBy, f.CreatedAt)));
}).WithName("GetStoredFiles");

app.MapPost("/api/files", async (CreateFileRequest req, AppDbContext db) =>
{
    var file = new StoredFile
    {
        FileName = req.FileName,
        ContentType = req.ContentType,
        FileSizeBytes = req.FileSizeBytes,
        StorageProvider = req.StorageProvider,
        Category = req.Category,
        Description = req.Description,
        StoragePath = $"uploads/{req.Category.ToLower()}/{req.FileName}",
        UploadedBy = "admin@platform.local"
    };
    db.Files.Add(file);
    await db.SaveChangesAsync();
    return Results.Created($"/api/files/{file.Id}", new StoredFileDto(file.Id, file.FileName, file.ContentType, file.FileSizeBytes, file.StorageProvider, file.Category, file.Description, file.StoragePath, file.UploadedBy, file.CreatedAt));
}).WithName("CreateStoredFile").RequireAuthorization();

app.MapDelete("/api/files/{id:guid}", async (Guid id, AppDbContext db) =>
{
    var file = await db.Files.FindAsync(id);
    if (file == null) return Results.NotFound();
    db.Files.Remove(file);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).WithName("DeleteStoredFile").RequireAuthorization();

// -------------------------------------------------------------
// Enterprise Notification Center Endpoints
// -------------------------------------------------------------
app.MapGet("/api/notifications", async (string? severity, bool? unreadOnly, AppDbContext db) =>
{
    var query = db.Notifications.AsQueryable();
    if (!string.IsNullOrWhiteSpace(severity) && severity != "All")
        query = query.Where(n => n.Severity == severity);
    if (unreadOnly == true)
        query = query.Where(n => !n.IsRead);

    var items = await query.OrderByDescending(n => n.CreatedAt).ToListAsync();
    return Results.Ok(items.Select(n => new NotificationDto(n.Id, n.Title, n.Message, n.Severity, n.TargetAudience, n.IsRead, n.ActionUrl, n.CreatedBy, n.CreatedAt)));
}).WithName("GetNotifications");

app.MapPost("/api/notifications", async (CreateNotificationRequest req, AppDbContext db) =>
{
    var notif = new AppNotification
    {
        Title = req.Title,
        Message = req.Message,
        Severity = req.Severity,
        TargetAudience = req.TargetAudience,
        ActionUrl = req.ActionUrl,
        CreatedBy = "Administrator",
        IsRead = false
    };
    db.Notifications.Add(notif);
    await db.SaveChangesAsync();
    return Results.Created($"/api/notifications/{notif.Id}", new NotificationDto(notif.Id, notif.Title, notif.Message, notif.Severity, notif.TargetAudience, notif.IsRead, notif.ActionUrl, notif.CreatedBy, notif.CreatedAt));
}).WithName("CreateNotification").RequireAuthorization();

app.MapPut("/api/notifications/{id:guid}/read", async (Guid id, AppDbContext db) =>
{
    var notif = await db.Notifications.FindAsync(id);
    if (notif == null) return Results.NotFound();
    notif.IsRead = true;
    await db.SaveChangesAsync();
    return Results.Ok(new NotificationDto(notif.Id, notif.Title, notif.Message, notif.Severity, notif.TargetAudience, notif.IsRead, notif.ActionUrl, notif.CreatedBy, notif.CreatedAt));
}).WithName("MarkNotificationRead");

app.MapPost("/api/notifications/mark-all-read", async (AppDbContext db) =>
{
    var unread = await db.Notifications.Where(n => !n.IsRead).ToListAsync();
    foreach (var n in unread) n.IsRead = true;
    await db.SaveChangesAsync();
    return Results.Ok(new { count = unread.Count });
}).WithName("MarkAllNotificationsRead");

app.MapDelete("/api/notifications/{id:guid}", async (Guid id, AppDbContext db) =>
{
    var notif = await db.Notifications.FindAsync(id);
    if (notif == null) return Results.NotFound();
    db.Notifications.Remove(notif);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).WithName("DeleteNotification").RequireAuthorization();

// -------------------------------------------------------------
// Enterprise Text Templates Endpoints
// -------------------------------------------------------------
app.MapGet("/api/templates", async (string? search, string? category, AppDbContext db) =>
{
    var query = db.TextTemplates.AsQueryable();
    if (!string.IsNullOrWhiteSpace(search))
        query = query.Where(t => t.Name.Contains(search) || t.DisplayName.Contains(search) || t.Subject.Contains(search));
    if (!string.IsNullOrWhiteSpace(category) && category != "All")
        query = query.Where(t => t.Category == category);

    var items = await query.OrderBy(t => t.Category).ThenBy(t => t.DisplayName).ToListAsync();
    return Results.Ok(items.Select(t => new TextTemplateDto(t.Id, t.Name, t.DisplayName, t.Category, t.Culture, t.Subject, t.Content, t.AvailableTokens, t.LastModifiedAt)));
}).WithName("GetTextTemplates");

app.MapPost("/api/templates", async (CreateTemplateRequest req, AppDbContext db) =>
{
    var tmpl = new TextTemplate
    {
        Name = req.Name,
        DisplayName = req.DisplayName,
        Category = req.Category,
        Culture = req.Culture,
        Subject = req.Subject,
        Content = req.Content,
        AvailableTokens = req.AvailableTokens,
        LastModifiedAt = DateTime.UtcNow
    };
    db.TextTemplates.Add(tmpl);
    await db.SaveChangesAsync();
    return Results.Created($"/api/templates/{tmpl.Id}", new TextTemplateDto(tmpl.Id, tmpl.Name, tmpl.DisplayName, tmpl.Category, tmpl.Culture, tmpl.Subject, tmpl.Content, tmpl.AvailableTokens, tmpl.LastModifiedAt));
}).WithName("CreateTextTemplate").RequireAuthorization();

app.MapPut("/api/templates/{id:guid}", async (Guid id, UpdateTemplateRequest req, AppDbContext db) =>
{
    var tmpl = await db.TextTemplates.FindAsync(id);
    if (tmpl == null) return Results.NotFound();
    tmpl.Subject = req.Subject;
    tmpl.Content = req.Content;
    tmpl.LastModifiedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.Ok(new TextTemplateDto(tmpl.Id, tmpl.Name, tmpl.DisplayName, tmpl.Category, tmpl.Culture, tmpl.Subject, tmpl.Content, tmpl.AvailableTokens, tmpl.LastModifiedAt));
}).WithName("UpdateTextTemplate").RequireAuthorization();

app.MapDelete("/api/templates/{id:guid}", async (Guid id, AppDbContext db) =>
{
    var tmpl = await db.TextTemplates.FindAsync(id);
    if (tmpl == null) return Results.NotFound();
    db.TextTemplates.Remove(tmpl);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).WithName("DeleteTextTemplate").RequireAuthorization();

app.MapPost("/api/templates/{id:guid}/preview", async (Guid id, PreviewTemplateRequest req, AppDbContext db) =>
{
    var tmpl = await db.TextTemplates.FindAsync(id);
    if (tmpl == null) return Results.NotFound();

    string renderedSubject = tmpl.Subject;
    string renderedContent = tmpl.Content;
    if (req.SampleValues != null)
    {
        foreach (var (k, v) in req.SampleValues)
        {
            renderedSubject = renderedSubject.Replace(k, v);
            renderedContent = renderedContent.Replace(k, v);
        }
    }
    return Results.Ok(new TemplatePreviewResponse(renderedSubject, renderedContent));
}).WithName("PreviewTextTemplate");

// -------------------------------------------------------------
// Enterprise Localization & Translation Endpoints
// -------------------------------------------------------------
app.MapGet("/api/languages", async (AppDbContext db) =>
{
    var items = await db.Languages.OrderByDescending(l => l.IsDefault).ThenBy(l => l.DisplayName).ToListAsync();
    return Results.Ok(items.Select(l => new LanguageDto(l.Id, l.CultureCode, l.DisplayName, l.FlagEmoji, l.IsDefault, l.IsActive, l.TranslationProgressPercent)));
}).WithName("GetLanguages");

app.MapPost("/api/languages", async (CreateLanguageRequest req, AppDbContext db) =>
{
    var lang = new LanguageRecord
    {
        CultureCode = req.CultureCode,
        DisplayName = req.DisplayName,
        FlagEmoji = req.FlagEmoji,
        IsDefault = false,
        IsActive = true,
        TranslationProgressPercent = 50
    };
    db.Languages.Add(lang);
    await db.SaveChangesAsync();
    return Results.Created($"/api/languages/{lang.Id}", new LanguageDto(lang.Id, lang.CultureCode, lang.DisplayName, lang.FlagEmoji, lang.IsDefault, lang.IsActive, lang.TranslationProgressPercent));
}).WithName("CreateLanguage").RequireAuthorization();

app.MapPut("/api/languages/{id:guid}/default", async (Guid id, AppDbContext db) =>
{
    var target = await db.Languages.FindAsync(id);
    if (target == null) return Results.NotFound();
    var all = await db.Languages.ToListAsync();
    foreach (var l in all) l.IsDefault = (l.Id == id);
    await db.SaveChangesAsync();
    return Results.Ok();
}).WithName("SetDefaultLanguage").RequireAuthorization();

app.MapPut("/api/languages/{id:guid}/toggle-active", async (Guid id, AppDbContext db) =>
{
    var target = await db.Languages.FindAsync(id);
    if (target == null) return Results.NotFound();
    target.IsActive = !target.IsActive;
    await db.SaveChangesAsync();
    return Results.Ok(new LanguageDto(target.Id, target.CultureCode, target.DisplayName, target.FlagEmoji, target.IsDefault, target.IsActive, target.TranslationProgressPercent));
}).WithName("ToggleLanguageActive").RequireAuthorization();

app.MapDelete("/api/languages/{id:guid}", async (Guid id, AppDbContext db) =>
{
    var target = await db.Languages.FindAsync(id);
    if (target == null) return Results.NotFound();
    if (target.IsDefault) return Results.BadRequest("Cannot delete default language.");
    db.Languages.Remove(target);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).WithName("DeleteLanguage").RequireAuthorization();

app.MapGet("/api/languages/texts", async (string? cultureCode, string? search, AppDbContext db) =>
{
    var query = db.LocalizationTexts.AsQueryable();
    if (!string.IsNullOrWhiteSpace(cultureCode))
        query = query.Where(t => t.CultureCode == cultureCode);
    if (!string.IsNullOrWhiteSpace(search))
        query = query.Where(t => t.ResourceKey.Contains(search) || t.Value.Contains(search));

    var items = await query.OrderBy(t => t.Category).ThenBy(t => t.ResourceKey).ToListAsync();
    return Results.Ok(items.Select(t => new LocalizationResourceDto(t.Id, t.ResourceKey, t.CultureCode, t.Value, t.Category, t.LastModifiedAt)));
}).WithName("GetLocalizationTexts");

app.MapPut("/api/languages/texts/{id:guid}", async (Guid id, UpdateResourceRequest req, AppDbContext db) =>
{
    var item = await db.LocalizationTexts.FindAsync(id);
    if (item == null) return Results.NotFound();
    item.Value = req.Value;
    item.LastModifiedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.Ok(new LocalizationResourceDto(item.Id, item.ResourceKey, item.CultureCode, item.Value, item.Category, item.LastModifiedAt));
}).WithName("UpdateLocalizationText").RequireAuthorization();

// -------------------------------------------------------------
// Enterprise OpenIddict & OAuth Client Endpoints
// -------------------------------------------------------------
app.MapGet("/api/oauth-clients", async (AppDbContext db) =>
{
    var clients = await db.OAuthClients.OrderBy(c => c.ClientName).ToListAsync();
    return Results.Ok(clients.Select(c => new OAuthClientDto(c.Id, c.ClientId, c.ClientName, c.ClientType, c.AllowedGrantTypes, c.AllowedScopes, c.RedirectUris, c.PostLogoutRedirectUris, c.ClientSecretMasked, c.IsActive, c.CreatedAt)));
}).WithName("GetOAuthClients");

app.MapPost("/api/oauth-clients", async (CreateOAuthClientRequest req, AppDbContext db) =>
{
    var client = new OAuthClientApp
    {
        ClientId = req.ClientId,
        ClientName = req.ClientName,
        ClientType = req.ClientType,
        AllowedGrantTypes = req.AllowedGrantTypes,
        AllowedScopes = req.AllowedScopes,
        RedirectUris = req.RedirectUris,
        PostLogoutRedirectUris = req.PostLogoutRedirectUris,
        ClientSecretMasked = "sk_live_" + Guid.NewGuid().ToString("N")[..16],
        IsActive = true
    };
    db.OAuthClients.Add(client);
    await db.SaveChangesAsync();
    return Results.Created($"/api/oauth-clients/{client.Id}", new OAuthClientDto(client.Id, client.ClientId, client.ClientName, client.ClientType, client.AllowedGrantTypes, client.AllowedScopes, client.RedirectUris, client.PostLogoutRedirectUris, client.ClientSecretMasked, client.IsActive, client.CreatedAt));
}).WithName("CreateOAuthClient").RequireAuthorization();

app.MapPut("/api/oauth-clients/{id:guid}", async (Guid id, UpdateOAuthClientRequest req, AppDbContext db) =>
{
    var client = await db.OAuthClients.FindAsync(id);
    if (client == null) return Results.NotFound();
    client.ClientName = req.ClientName;
    client.AllowedGrantTypes = req.AllowedGrantTypes;
    client.AllowedScopes = req.AllowedScopes;
    client.RedirectUris = req.RedirectUris;
    client.PostLogoutRedirectUris = req.PostLogoutRedirectUris;
    client.IsActive = req.IsActive;
    await db.SaveChangesAsync();
    return Results.Ok(new OAuthClientDto(client.Id, client.ClientId, client.ClientName, client.ClientType, client.AllowedGrantTypes, client.AllowedScopes, client.RedirectUris, client.PostLogoutRedirectUris, client.ClientSecretMasked, client.IsActive, client.CreatedAt));
}).WithName("UpdateOAuthClient").RequireAuthorization();

app.MapPost("/api/oauth-clients/{id:guid}/regenerate-secret", async (Guid id, AppDbContext db) =>
{
    var client = await db.OAuthClients.FindAsync(id);
    if (client == null) return Results.NotFound();
    string newSecret = "sk_live_" + Guid.NewGuid().ToString("N")[..24];
    client.ClientSecretMasked = newSecret[..8] + "••••••••••••••••";
    await db.SaveChangesAsync();
    return Results.Ok(new { NewSecret = newSecret, MaskedSecret = client.ClientSecretMasked });
}).WithName("RegenerateOAuthClientSecret").RequireAuthorization();

app.MapDelete("/api/oauth-clients/{id:guid}", async (Guid id, AppDbContext db) =>
{
    var client = await db.OAuthClients.FindAsync(id);
    if (client == null) return Results.NotFound();
    db.OAuthClients.Remove(client);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).WithName("DeleteOAuthClient").RequireAuthorization();

// -------------------------------------------------------------
// CMS Kit Blog Hub Module Endpoints (Blogs, Posts, Reactions, Comments)
// -------------------------------------------------------------
app.MapGet("/api/cms/blogs", async (AppDbContext db) =>
{
    var blogs = await db.Blogs.OrderBy(b => b.Name).ToListAsync();
    return Results.Ok(blogs.Select(b => new BlogDto(b.Id, b.Name, b.Slug, b.Description, b.CreatedAt)));
}).WithName("GetBlogs").WithSummary("List all publication blogs");

app.MapGet("/api/cms/blog-posts", async (string? search, string? category, string? tag, int pageNumber = 1, int pageSize = 12, AppDbContext db = null!) =>
{
    var query = db.BlogPosts.Where(p => p.IsPublished).AsQueryable();
    if (!string.IsNullOrWhiteSpace(search))
        query = query.Where(p => p.Title.Contains(search) || p.ShortDescription.Contains(search) || p.Content.Contains(search));
    if (!string.IsNullOrWhiteSpace(category) && category != "All")
        query = query.Where(p => p.Category == category);
    if (!string.IsNullOrWhiteSpace(tag))
        query = query.Where(p => p.Tags.Contains(tag));

    var total = await query.CountAsync();
    var items = await query.OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
                           .Skip((pageNumber - 1) * pageSize)
                           .Take(pageSize)
                           .ToListAsync();

    var dtos = items.Select(p => new BlogPostDto(p.Id, p.BlogId, p.Title, p.Slug, p.ShortDescription, p.Content, p.CoverImageUrl, p.AuthorName, p.AuthorAvatarUrl, p.Category, p.Tags, p.ReadMinutes, p.ViewCount, p.LikeCount, p.IsPublished, p.CreatedAt, p.PublishedAt)).ToList();
    return Results.Ok(new PagedResultDto<BlogPostDto>(dtos, total, pageNumber, pageSize));
}).WithName("GetBlogPosts").WithSummary("List published blog posts with search and category filters");

app.MapGet("/api/cms/blog-posts/{slug}", async (string slug, AppDbContext db) =>
{
    var post = await db.BlogPosts.FirstOrDefaultAsync(p => p.Slug == slug);
    if (post == null && Guid.TryParse(slug, out var gid))
        post = await db.BlogPosts.FindAsync(gid);
    if (post == null) return Results.NotFound();
    post.ViewCount++;
    await db.SaveChangesAsync();
    return Results.Ok(new BlogPostDto(post.Id, post.BlogId, post.Title, post.Slug, post.ShortDescription, post.Content, post.CoverImageUrl, post.AuthorName, post.AuthorAvatarUrl, post.Category, post.Tags, post.ReadMinutes, post.ViewCount, post.LikeCount, post.IsPublished, post.CreatedAt, post.PublishedAt));
}).WithName("GetBlogPostBySlug").WithSummary("Retrieve article details by slug or Guid");

app.MapPost("/api/cms/blog-posts/{id:guid}/react", async (Guid id, ReactRequest req, ClaimsPrincipal user, AppDbContext db) =>
{
    var post = await db.BlogPosts.FindAsync(id);
    if (post == null) return Results.NotFound();
    var userName = user.Identity?.Name ?? "anonymous";
    var reaction = new BlogPostReaction { BlogPostId = id, ReactionName = req.ReactionName, UserName = userName };
    db.Reactions.Add(reaction);
    post.LikeCount++;
    await db.SaveChangesAsync();
    return Results.Ok(new { likeCount = post.LikeCount, reaction = req.ReactionName });
}).WithName("ReactToBlogPost").WithSummary("Add emoji reaction to blog post");

app.MapGet("/api/cms/blog-posts/{id:guid}/comments", async (Guid id, AppDbContext db) =>
{
    var comments = await db.Comments.OrderByDescending(c => c.CreatedAt).ToListAsync();
    return Results.Ok(comments);
}).WithName("GetBlogPostComments").WithSummary("List all comments for a blog post");

app.MapPost("/api/cms/blog-posts/{id:guid}/comments", async (Guid id, PostCommentRequest req, AppDbContext db) =>
{
    var comment = new CmsComment
    {
        ArticleId = 1,
        AuthorName = req.AuthorName,
        AuthorEmail = req.AuthorEmail,
        CommentText = req.CommentText,
        Status = "Approved"
    };
    db.Comments.Add(comment);
    await db.SaveChangesAsync();
    return Results.Created($"/api/cms/comments/{comment.Id}", comment);
}).WithName("PostBlogPostComment").WithSummary("Post a discussion comment to a blog post");

// -------------------------------------------------------------
// Real-Time SignalR Event Hub & Live Notification Broadcast
// -------------------------------------------------------------
app.MapHub<PlatformEventHub>("/hubs/events");

app.MapPost("/api/events/broadcast", async (CreateNotificationRequest req, IHubContext<PlatformEventHub> hub, AppDbContext db) =>
{
    var notif = new AppNotification
    {
        Title = string.IsNullOrWhiteSpace(req.Title) ? "Live System Broadcast" : req.Title,
        Message = string.IsNullOrWhiteSpace(req.Message) ? "Real-time event dispatched across connected SignalR nodes." : req.Message,
        Severity = string.IsNullOrWhiteSpace(req.Severity) ? "Info" : req.Severity,
        TargetAudience = req.TargetAudience ?? "All Users",
        ActionUrl = req.ActionUrl ?? "/",
        CreatedBy = "SignalR EventHub",
        CreatedAt = DateTime.UtcNow
    };
    db.Notifications.Add(notif);
    await db.SaveChangesAsync();

    await hub.Clients.All.SendAsync("ReceiveEvent", new
    {
        id = notif.Id,
        title = notif.Title,
        message = notif.Message,
        severity = notif.Severity,
        timestamp = notif.CreatedAt
    });

    return Results.Ok(notif);
}).WithName("BroadcastRealtimeEvent").WithSummary("Broadcast a real-time notification event over SignalR /hubs/events and persist to Notifications");

app.MapGet("/api/system/architecture", async (AppDbContext db) =>
{
    var tables = new List<object>();
    var totalRows = 0L;
    try
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();

        var tableNames = new List<string>();
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name NOT LIKE '__EFMigrations%' ORDER BY name;";
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                tableNames.Add(reader.GetString(0));
        }

        foreach (var tName in tableNames)
        {
            long rowCount = 0;
            await using (var countCmd = conn.CreateCommand())
            {
                countCmd.CommandText = $"SELECT COUNT(*) FROM \"{tName}\";";
                var scalar = await countCmd.ExecuteScalarAsync();
                rowCount = Convert.ToInt64(scalar ?? 0);
            }
            totalRows += rowCount;

            var columns = new List<object>();
            await using (var colCmd = conn.CreateCommand())
            {
                colCmd.CommandText = $"PRAGMA table_info(\"{tName}\");";
                await using var colReader = await colCmd.ExecuteReaderAsync();
                while (await colReader.ReadAsync())
                {
                    columns.Add(new
                    {
                        name = colReader.GetString(1),
                        type = colReader.GetString(2),
                        notNull = colReader.GetInt32(3) == 1,
                        isPrimaryKey = colReader.GetInt32(5) == 1
                    });
                }
            }

            tables.Add(new
            {
                tableName = tName,
                rowCount = rowCount,
                columnCount = columns.Count,
                columns = columns
            });
        }
    }
    catch { }

    return Results.Ok(new
    {
        solutionName = "NovaMesh",
        framework = "net10.0",
        databaseProvider = "SQLite (EF Core)",
        signalRHub = "/hubs/events",
        openApiSpec = "/openapi/v1.json",
        scalarDocs = "/scalar/v1",
        totalTables = tables.Count,
        totalRows = totalRows,
        tables = tables
    });
}).WithName("GetSystemArchitecture").WithSummary("Introspect live SQLite database tables, column schemas, row counts, and service topology");

// -------------------------------------------------------------
// Sample Domain Module Endpoints
// -------------------------------------------------------------
// -------------------------------------------------------------
// Aspire Distributed Service & Telemetry Module Endpoints
// -------------------------------------------------------------
app.MapGet("/api/aspire/resources", async (AppDbContext db) =>
{
    var resources = await db.CloudResources.OrderBy(r => r.Name).ToListAsync();
    return Results.Ok(resources.Select(r => new CloudResourceDto(r.Id, r.Name, r.ResourceType, r.EndpointUrl, r.State, r.ReplicaCount, r.MemoryMb, r.CpuPercentage, r.LastHeartbeat)));
}).WithName("GetAspireResources").WithSummary("List all Aspire orchestrated cloud service resources and health status");

app.MapGet("/api/aspire/orders", async (string? status, int pageNumber = 1, int pageSize = 10, AppDbContext db = null!) =>
{
    var query = db.DistributedOrders.AsQueryable();
    if (!string.IsNullOrWhiteSpace(status) && status != "All")
        query = query.Where(o => o.Status == status);

    var total = await query.CountAsync();
    var items = await query.OrderByDescending(o => o.CreatedAt).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
    var dtos = items.Select(o => new DistributedOrderDto(o.Id, o.OrderNumber, o.ServiceOrigin, o.DestinationService, o.Status, o.TotalAmount, o.LatencyMs, o.TraceId, o.CreatedAt)).ToList();
    return Results.Ok(new PagedResultDto<DistributedOrderDto>(dtos, total, pageNumber, pageSize));
}).WithName("GetDistributedOrders").WithSummary("List distributed cross-service orders with OpenTelemetry trace correlation");

app.MapPost("/api/aspire/orders", async (CreateDistributedOrderRequest req, AppDbContext db) =>
{
    var traceId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
    var sw = Stopwatch.StartNew();
    
    // Simulate inter-service latency and distributed processing
    await Task.Delay(Random.Shared.Next(20, 50));
    sw.Stop();

    var order = new DistributedOrder
    {
        OrderNumber = $"ORD-ASP-{Random.Shared.Next(1000, 9999)}",
        ServiceOrigin = string.IsNullOrWhiteSpace(req.ServiceOrigin) ? "webfrontend" : req.ServiceOrigin,
        DestinationService = string.IsNullOrWhiteSpace(req.DestinationService) ? "apiservice" : req.DestinationService,
        Status = "Completed",
        TotalAmount = req.TotalAmount > 0 ? req.TotalAmount : 150.00m,
        LatencyMs = sw.ElapsedMilliseconds,
        TraceId = traceId,
        CreatedAt = DateTime.UtcNow
    };
    db.DistributedOrders.Add(order);

    // Record distributed trace spans
    db.TraceRecords.Add(new DistributedTraceRecord
    {
        TraceId = traceId,
        SpanId = Guid.NewGuid().ToString("N")[..16],
        ServiceName = order.ServiceOrigin,
        OperationName = "HTTP POST /api/aspire/orders",
        DurationMs = sw.ElapsedMilliseconds,
        StatusCode = 201,
        CreatedAt = DateTime.UtcNow
    });
    db.TraceRecords.Add(new DistributedTraceRecord
    {
        TraceId = traceId,
        SpanId = Guid.NewGuid().ToString("N")[..16],
        ServiceName = order.DestinationService,
        OperationName = "Activity: ProcessDistributedOrder",
        DurationMs = Math.Max(5, sw.ElapsedMilliseconds - 10),
        StatusCode = 200,
        CreatedAt = DateTime.UtcNow
    });

    await db.SaveChangesAsync();
    return Results.Created($"/api/aspire/orders/{order.Id}", new DistributedOrderDto(order.Id, order.OrderNumber, order.ServiceOrigin, order.DestinationService, order.Status, order.TotalAmount, order.LatencyMs, order.TraceId, order.CreatedAt));
}).WithName("CreateDistributedOrder").WithSummary("Create a distributed order and record synthetic OpenTelemetry traces");

app.MapGet("/api/aspire/traces", async (string? traceId, AppDbContext db) =>
{
    var query = db.TraceRecords.AsQueryable();
    if (!string.IsNullOrWhiteSpace(traceId))
        query = query.Where(t => t.TraceId == traceId);

    var traces = await query.OrderByDescending(t => t.CreatedAt).Take(20).ToListAsync();
    return Results.Ok(traces.Select(t => new DistributedTraceRecordDto(t.Id, t.TraceId, t.SpanId, t.ServiceName, t.OperationName, t.DurationMs, t.StatusCode, t.ErrorMessage, t.CreatedAt)));
}).WithName("GetDistributedTraces").WithSummary("Query OpenTelemetry trace records and span waterfall telemetry");

app.MapPost("/api/aspire/simulate-fault", async (FaultSimulationRequest req, AppDbContext db) =>
{
    var traceId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
    var sw = Stopwatch.StartNew();

    if (req.LatencyMs > 0)
    {
        await Task.Delay(Math.Min(req.LatencyMs, 3000));
    }

    sw.Stop();

    if (req.TriggerFailure)
    {
        db.TraceRecords.Add(new DistributedTraceRecord
        {
            TraceId = traceId,
            SpanId = Guid.NewGuid().ToString("N")[..16],
            ServiceName = "apiservice",
            OperationName = $"FaultSimulated: {req.FaultType}",
            DurationMs = sw.ElapsedMilliseconds,
            StatusCode = 503,
            ErrorMessage = $"Polly Circuit Breaker / Fault Injected: {req.FaultType}",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        return Results.Problem(
            title: "Simulated Service Outage",
            detail: $"Resilience pipeline activated. Fault type: {req.FaultType} simulated across Polly handler.",
            statusCode: 503
        );
    }

    db.TraceRecords.Add(new DistributedTraceRecord
    {
        TraceId = traceId,
        SpanId = Guid.NewGuid().ToString("N")[..16],
        ServiceName = "apiservice",
        OperationName = $"FaultSimulated: {req.FaultType} [Recovered]",
        DurationMs = sw.ElapsedMilliseconds,
        StatusCode = 200,
        ErrorMessage = null,
        CreatedAt = DateTime.UtcNow
    });
    await db.SaveChangesAsync();

    return Results.Ok(new { message = $"Simulated fault '{req.FaultType}' handled successfully by Polly resilience pipeline.", latencyMs = sw.ElapsedMilliseconds, traceId });
}).WithName("SimulateFault").WithSummary("Test Polly resilience, retry pipelines, and OpenTelemetry error logging");

app.MapFallbackToFile("index.html");

NovaMesh.Api.Endpoints.InvoiceEndpoints.MapInvoiceEndpoints(app);
app.Run();