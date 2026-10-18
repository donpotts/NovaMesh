using Microsoft.EntityFrameworkCore;
using NovaMesh.Data;
using NovaMesh.Data.Entities;

namespace NovaMesh.Api.Endpoints;

public static class InvoiceEndpoints
{
    private static bool _tableInitialized = false;

    private static async Task EnsureTableAndSeedAsync(AppDbContext db)
    {
        if (_tableInitialized) return;
        try
        {
            await db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS Invoices (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    InvoiceNumber TEXT NOT NULL DEFAULT '', CustomerName TEXT NOT NULL DEFAULT '', Amount REAL NOT NULL DEFAULT 0, IsPaid INTEGER NOT NULL DEFAULT 1, IssuedAt TEXT NOT NULL DEFAULT '',
                    CreatedAt TEXT NOT NULL
                );");

            if (!await db.Invoices.AnyAsync())
            {
                db.Invoices.AddRange(
                    new Invoice { InvoiceNumber = "Enterprise Invoice Alpha", CustomerName = "Enterprise Invoice Alpha", Amount = 1250.00m, IsPaid = true, IssuedAt = "Enterprise Invoice Alpha", CreatedAt = DateTime.UtcNow.AddDays(-3) },
                    new Invoice { InvoiceNumber = "Cloud Invoice Beta", CustomerName = "Cloud Invoice Beta", Amount = 3490.50m, IsPaid = true, IssuedAt = "Cloud Invoice Beta", CreatedAt = DateTime.UtcNow.AddDays(-2) },
                    new Invoice { InvoiceNumber = "Global Invoice Gamma", CustomerName = "Global Invoice Gamma", Amount = 895.00m, IsPaid = false, IssuedAt = "Global Invoice Gamma", CreatedAt = DateTime.UtcNow.AddDays(-1) }
                );
                await db.SaveChangesAsync();
            }
            _tableInitialized = true;
        }
        catch { }
    }

    public static void MapInvoiceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/entities/invoices").WithTags("Invoices");

        group.MapGet("/", async (string? search, AppDbContext db) =>
        {
            await EnsureTableAndSeedAsync(db);
            var query = db.Invoices.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x => EF.Property<string>(x, "InvoiceNumber").Contains(search));
            }
            var items = await query.OrderByDescending(x => x.Id).ToListAsync();
            return Results.Ok(items);
        }).WithName("GetAllInvoices").WithSummary("List all Invoices with optional search filter");

        group.MapGet("/{id:int}", async (int id, AppDbContext db) =>
        {
            await EnsureTableAndSeedAsync(db);
            var item = await db.Invoices.FindAsync(id);
            return item is not null ? Results.Ok(item) : Results.NotFound();
        }).WithName("GetInvoiceById").WithSummary("Get Invoice by ID");

        group.MapPost("/", async (Invoice input, AppDbContext db) =>
        {
            await EnsureTableAndSeedAsync(db);
            input.CreatedAt = DateTime.UtcNow;
            db.Invoices.Add(input);
            await db.SaveChangesAsync();
            return Results.Created($"/api/entities/invoices/{input.Id}", input);
        }).WithName("CreateInvoice").WithSummary("Create a new Invoice record");

        group.MapPut("/{id:int}", async (int id, Invoice input, AppDbContext db) =>
        {
            await EnsureTableAndSeedAsync(db);
            var existing = await db.Invoices.FindAsync(id);
            if (existing is null) return Results.NotFound();
            existing.InvoiceNumber = input.InvoiceNumber;
            existing.CustomerName = input.CustomerName;
            existing.Amount = input.Amount;
            existing.IsPaid = input.IsPaid;
            existing.IssuedAt = input.IssuedAt;
            await db.SaveChangesAsync();
            return Results.Ok(existing);
        }).WithName("UpdateInvoice").WithSummary("Update an existing Invoice record");

        group.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
        {
            await EnsureTableAndSeedAsync(db);
            var existing = await db.Invoices.FindAsync(id);
            if (existing is null) return Results.NotFound();
            db.Invoices.Remove(existing);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).WithName("DeleteInvoice").WithSummary("Delete a Invoice record");

        group.MapPost("/seed-batch", async (AppDbContext db) =>
        {
            await EnsureTableAndSeedAsync(db);
            var batch = new[]
            {
                new Invoice { InvoiceNumber = "Enterprise Invoice Alpha", CustomerName = "Enterprise Invoice Alpha", Amount = 1250.00m, IsPaid = true, IssuedAt = "Enterprise Invoice Alpha", CreatedAt = DateTime.UtcNow },
                new Invoice { InvoiceNumber = "Cloud Invoice Beta", CustomerName = "Cloud Invoice Beta", Amount = 3490.50m, IsPaid = true, IssuedAt = "Cloud Invoice Beta", CreatedAt = DateTime.UtcNow.AddMinutes(-5) },
                new Invoice { InvoiceNumber = "Global Invoice Gamma", CustomerName = "Global Invoice Gamma", Amount = 895.00m, IsPaid = false, IssuedAt = "Global Invoice Gamma", CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
                new Invoice { InvoiceNumber = "Enterprise Invoice Alpha", CustomerName = "Enterprise Invoice Alpha", Amount = 1250.00m, IsPaid = true, IssuedAt = "Enterprise Invoice Alpha", CreatedAt = DateTime.UtcNow.AddMinutes(-15) },
                new Invoice { InvoiceNumber = "Cloud Invoice Beta", CustomerName = "Cloud Invoice Beta", Amount = 3490.50m, IsPaid = true, IssuedAt = "Cloud Invoice Beta", CreatedAt = DateTime.UtcNow.AddMinutes(-20) }
            };
            db.Invoices.AddRange(batch);
            await db.SaveChangesAsync();
            return Results.Ok(new { added = batch.Length, total = await db.Invoices.CountAsync() });
        }).WithName("SeedBatchInvoices").WithSummary("Bulk-seed 5 sample Invoice rows");

        group.MapGet("/export", async (string? format, AppDbContext db, HttpContext http) =>
        {
            await EnsureTableAndSeedAsync(db);
            var rows = await db.Invoices.OrderByDescending(x => x.Id).ToListAsync();
            if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
            {
                var props = typeof(Invoice).GetProperties();
                var sb = new System.Text.StringBuilder();
                sb.AppendLine(string.Join(",", props.Select(p => p.Name)));
                foreach (var r in rows)
                {
                    var vals = props.Select(p => $"\"{(p.GetValue(r)?.ToString() ?? "").Replace("\"", "\"\"")}\"");
                    sb.AppendLine(string.Join(",", vals));
                }
                var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
                http.Response.Headers.Append("Content-Disposition", "attachment; filename=\"invoices-export.csv\"");
                return Results.File(bytes, "text/csv; charset=utf-8");
            }
            else
            {
                var json = System.Text.Json.JsonSerializer.Serialize(rows, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                http.Response.Headers.Append("Content-Disposition", "attachment; filename=\"invoices-export.json\"");
                return Results.File(bytes, "application/json; charset=utf-8");
            }
        }).WithName("ExportInvoices").WithSummary("Export all Invoices as CSV or JSON");
    }
}