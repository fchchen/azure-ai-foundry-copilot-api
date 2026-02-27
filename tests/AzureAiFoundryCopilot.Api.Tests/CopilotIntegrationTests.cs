using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

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
        Assert.Equal("OAuth", ap.GetProperty("authType").GetString());
        var functionNames = ap.GetProperty("functions").EnumerateArray()
            .Select(f => f.GetProperty("name").GetString())
            .ToList();
        Assert.Contains("summarizeMyUnreadEmails", functionNames);

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
        Assert.Equal("OAuth", json.GetProperty("runtimes")[0].GetProperty("auth").GetProperty("type").GetString());
    }

    [Fact]
    public async Task CopilotMetadata_UsesConfiguredTenantClientAndApiBaseUrl()
    {
        using var configuredFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["CopilotPlugin:ApiBaseUrl"] = "https://api.contoso.com",
                        ["EntraId:TenantId"] = "11111111-2222-3333-4444-555555555555",
                        ["EntraId:ClientId"] = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
                        ["EntraId:Audience"] = "api://aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"
                    });
                });
            });
        using var configuredClient = configuredFactory.CreateClient();

        var openApi = await configuredClient.GetFromJsonAsync<JsonElement>("/api/copilot/openapi");
        var aiPlugin = await configuredClient.GetFromJsonAsync<JsonElement>("/api/copilot/ai-plugin");

        Assert.Equal("https://api.contoso.com", openApi.GetProperty("servers")[0].GetProperty("url").GetString());
        Assert.Equal(
            "https://login.microsoftonline.com/11111111-2222-3333-4444-555555555555/oauth2/v2.0/authorize",
            openApi.GetProperty("components")
                .GetProperty("securitySchemes")
                .GetProperty("oauth2Auth")
                .GetProperty("flows")
                .GetProperty("authorizationCode")
                .GetProperty("authorizationUrl")
                .GetString());
        Assert.Equal(
            "api://aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/access_as_user",
            openApi.GetProperty("security")[0].GetProperty("oauth2Auth")[0].GetString());
        Assert.Equal(
            "api://aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/access_as_user",
            aiPlugin.GetProperty("runtimes")[0].GetProperty("auth").GetProperty("scope").GetString());
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

    [Fact]
    public async Task CopilotMetadataEndpoints_Return304WhenETagMatches()
    {
        var endpoints = new[] { "/api/copilot/openapi", "/api/copilot/ai-plugin" };

        foreach (var endpoint in endpoints)
        {
            var first = await _client.GetAsync(endpoint);
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            Assert.NotNull(first.Headers.ETag);

            using var secondRequest = new HttpRequestMessage(HttpMethod.Get, endpoint);
            secondRequest.Headers.TryAddWithoutValidation("If-None-Match", first.Headers.ETag!.Tag);
            var second = await _client.SendAsync(secondRequest);

            Assert.Equal(HttpStatusCode.NotModified, second.StatusCode);
        }
    }
}
