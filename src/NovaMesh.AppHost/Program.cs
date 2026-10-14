var builder = DistributedApplication.CreateBuilder(args);

// 1. SQLite Database Schema Migrator & Seed Worker
var migrator = builder.AddProject<Projects.NovaMesh_DbMigrator>("db-migrator");

// 2. Core Distributed API Service with Scalar OpenAPI & OpenTelemetry
var apiService = builder.AddProject<Projects.NovaMesh_Api>("apiservice")
    .WithReference(migrator)
    .WaitFor(migrator);

// 3. MudBlazor WebAssembly Frontend Client with Service Discovery
builder.AddProject<Projects.NovaMesh_Client>("webfrontend")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();