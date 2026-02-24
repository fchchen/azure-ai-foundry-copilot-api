using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AzureAiFoundryCopilot.Api.Tests;

public sealed class CopilotIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CopilotIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetManifest_Returns200WithExpectedStructure()
    {
        var response = await _client.GetAsync("/api/copilot/manifest");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("declarativeAgent", out var da));
        Assert.Equal("v1.6", da.GetProperty("schemaVersion").GetString());

        Assert.True(json.TryGetProperty("apiPlugin", out var ap));
        Assert.Equal("v2.4", ap.GetProperty("schemaVersion").GetString());

        Assert.True(json.TryGetProperty("developer", out _));
    }

    [Fact]
    public async Task GetOpenApi_Returns200WithOpenApi301()
    {
        var response = await _client.GetAsync("/api/copilot/openapi");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("3.0.1", json.GetProperty("openapi").GetString());
    }

    [Fact]
    public async Task GetAiPlugin_Returns200WithSchemaVersion()
    {
        var response = await _client.GetAsync("/api/copilot/ai-plugin");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("v2.4", json.GetProperty("schema_version").GetString());
    }

    [Fact]
    public async Task CopilotEndpoints_AllowAnonymousAccess()
    {
        var endpoints = new[] { "/api/copilot/manifest", "/api/copilot/openapi", "/api/copilot/ai-plugin" };
        foreach (var endpoint in endpoints)
        {
            var response = await _client.GetAsync(endpoint);
            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
