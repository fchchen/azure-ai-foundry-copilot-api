using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AzureAiFoundryCopilot.Application.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AzureAiFoundryCopilot.Api.Tests;

public sealed class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public IntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_Returns200()
    {
        var response = await _client.GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ok", json.GetProperty("status").GetString());
        Assert.Equal("azure-ai-foundry-copilot-api", json.GetProperty("service").GetString());

        if (json.TryGetProperty("services", out var services))
        {
            Assert.False(services.GetProperty("keyVaultEnabled").GetBoolean());
            Assert.False(services.GetProperty("blobStorageEnabled").GetBoolean());
            Assert.False(services.GetProperty("entraIdEnabled").GetBoolean());
            Assert.False(services.GetProperty("appInsightsEnabled").GetBoolean());
        }
    }

    [Fact]
    public async Task HealthzEndpoint_Returns200()
    {
        var response = await _client.GetAsync("/healthz");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Chat_WithEmptyPrompt_Returns400()
    {
        var request = new AiChatRequest("");
        var response = await _client.PostAsJsonAsync("/api/ai-foundry/chat", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Chat_WithOutOfRangeMaxTokens_Returns400()
    {
        var request = new AiChatRequest("Hello", MaxTokens: 0);
        var response = await _client.PostAsJsonAsync("/api/ai-foundry/chat", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Chat_WithValidPrompt_Returns200()
    {
        var request = new AiChatRequest("What is Azure AI Foundry?");
        var response = await _client.PostAsJsonAsync("/api/ai-foundry/chat", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<AiChatResponse>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.Completion);
    }
}
