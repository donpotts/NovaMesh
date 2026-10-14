using System.Diagnostics;
using System.Security.Claims;
using NovaMesh.Data;
using NovaMesh.Data.Entities;

namespace NovaMesh.Api.Middleware;

public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public AuditLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
    {
        var path = context.Request.Path.Value ?? "";
        // Skip noise endpoints
        if (path.StartsWith("/scalar") || path.StartsWith("/openapi") || path == "/favicon.ico")
        {
            await _next(context);
            return;
        }

        var sw = Stopwatch.StartNew();
        string? exceptionMessage = null;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            exceptionMessage = ex.Message;
            throw;
        }
        finally
        {
            sw.Stop();
            try
            {
                using var scope = serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var userName = context.User.Identity?.IsAuthenticated == true
                    ? context.User.Identity.Name ?? context.User.FindFirstValue(ClaimTypes.Name) ?? "authenticated"
                    : "anonymous";

                var entry = new AuditLogEntry
                {
                    ExecutionTime = DateTime.UtcNow,
                    UserName = userName,
                    HttpMethod = context.Request.Method,
                    Url = $"{context.Request.Path}{context.Request.QueryString}",
                    HttpStatusCode = context.Response.StatusCode,
                    DurationMs = sw.ElapsedMilliseconds,
                    ClientIpAddress = context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                    BrowserInfo = context.Request.Headers["User-Agent"].ToString(),
                    ActionName = $"{context.Request.Method} {context.Request.Path}",
                    Exception = exceptionMessage
                };

                db.AuditLogs.Add(entry);
                await db.SaveChangesAsync();
            }
            catch
            {
                // Never allow audit logging failure to crash request pipeline
            }
        }
    }
}