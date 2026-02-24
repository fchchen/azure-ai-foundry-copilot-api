using System.Net;
using System.Net.Http.Json;
using AzureAiFoundryCopilot.Application.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AzureAiFoundryCopilot.Api.Tests;

public sealed class ConversationsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ConversationsIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetConversation_WhenNotFound_Returns404()
    {
        var response = await _client.GetAsync("/api/conversations/nonexistent");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostConversation_ThenGet_Returns201And200()
    {
        var conversation = new ChatConversation("integration-test-1", "Hello", "World", DateTimeOffset.UtcNow);

        var postResponse = await _client.PostAsJsonAsync("/api/conversations", conversation);
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var getResponse = await _client.GetAsync("/api/conversations/integration-test-1");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var result = await getResponse.Content.ReadFromJsonAsync<ChatConversation>();
        Assert.NotNull(result);
        Assert.Equal("integration-test-1", result.ConversationId);
    }

    [Fact]
    public async Task GetRecentConversations_Returns200()
    {
        var response = await _client.GetAsync("/api/conversations/recent");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Chat_SavesConversationAutomatically()
    {
        var request = new AiChatRequest("What is Azure?");
        var chatResponse = await _client.PostAsJsonAsync("/api/ai-foundry/chat", request);
        Assert.Equal(HttpStatusCode.OK, chatResponse.StatusCode);

        var recentResponse = await _client.GetAsync("/api/conversations/recent?count=1");
        var recent = await recentResponse.Content.ReadFromJsonAsync<List<ChatConversation>>();
        Assert.NotNull(recent);
        Assert.NotEmpty(recent);
        Assert.Equal("What is Azure?", recent[0].UserPrompt);
    }
}
