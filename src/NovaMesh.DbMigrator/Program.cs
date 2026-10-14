using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using NovaMesh.Data;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("==========================================================================");
Console.WriteLine("  NovaMesh DbMigrator (ASPIRE Full Module Migration & Seeder)");
Console.WriteLine("==========================================================================");
Console.ResetColor();

var sw = Stopwatch.StartNew();
var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ?? "Data Source=app.db";
Console.WriteLine($"[DbMigrator] Target Connection: {connectionString}");

var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
optionsBuilder.UseSqlite(connectionString);

using var db = new AppDbContext(optionsBuilder.Options);

Console.WriteLine("[DbMigrator] Initializing database schema across all modules...");
await db.Database.EnsureCreatedAsync();

Console.WriteLine("[DbMigrator] Executing Data Seeders (Identity, Settings, AuditLogs, ASPIRE)...");
await DataSeeder.SeedAsync(db, "admin@nexus.local", "Admin123!", msg =>
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine(msg);
    Console.ResetColor();
});

sw.Stop();
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"[DbMigrator] ✓ Database migration and multi-module seeding completed in {sw.ElapsedMilliseconds} ms!");
Console.ResetColor();
return 0;