using System.Net;
using System.Net.Http.Json;
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
        Assert.StartsWith("[Mock]", result.Completion);
    }
}
