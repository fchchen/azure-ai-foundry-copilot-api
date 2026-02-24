using System.Net;
using System.Net.Http.Json;
using AzureAiFoundryCopilot.Application.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AzureAiFoundryCopilot.Api.Tests;

public sealed class AuthIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_AllowsAnonymous()
    {
        var response = await _client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CopilotManifest_AllowsAnonymous()
    {
        var response = await _client.GetAsync("/api/copilot/manifest");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ChatEndpoint_WithMockAuth_Returns200()
    {
        var request = new AiChatRequest("Test prompt");
        var response = await _client.PostAsJsonAsync("/api/ai-foundry/chat", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ConversationsEndpoint_WithMockAuth_Returns200()
    {
        var response = await _client.GetAsync("/api/conversations/recent");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
