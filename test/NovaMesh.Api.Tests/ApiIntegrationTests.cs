using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace NovaMesh.Api.Tests;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    [Fact]
    public async Task GetArticles_Public_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/cms/articles");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AdminLogin_WithDefaultCredentials_ReturnsJwtToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { UserName = "admin", Password = "Admin123!" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("token", content);
    }

    [Fact]
    public async Task GetSettings_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/settings");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditLogs_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/audit-logs");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/identity/users");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private record LoginTestResponse(string Token);
}