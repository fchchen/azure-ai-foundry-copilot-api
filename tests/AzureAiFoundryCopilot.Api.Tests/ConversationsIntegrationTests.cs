using System.Net;
using System.Net.Http.Json;
using AzureAiFoundryCopilot.Application.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AzureAiFoundryCopilot.Api.Tests;

public sealed class ConversationsIntegrationTests
{
    private static HttpClient CreateClient(out WebApplicationFactory<Program> factory)
    {
        factory = new WebApplicationFactory<Program>();
        return factory.CreateClient();
    }

    [Fact]
    public async Task GetConversation_WhenNotFound_Returns404()
    {
        using var client = CreateClient(out var factory);
        using (factory)
        {
            var response = await client.GetAsync("/api/conversations/nonexistent");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    [Fact]
    public async Task PostConversation_ThenGet_Returns201And200()
    {
        using var client = CreateClient(out var factory);
        using (factory)
        {
            var conversationId = $"integration-test-{Guid.NewGuid():N}";
            var conversation = new ChatConversation(conversationId, "Hello", "World", DateTimeOffset.UtcNow);

            var postResponse = await client.PostAsJsonAsync("/api/conversations", conversation);
            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            var getResponse = await client.GetAsync($"/api/conversations/{conversationId}");
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var result = await getResponse.Content.ReadFromJsonAsync<ChatConversation>();
            Assert.NotNull(result);
            Assert.Equal(conversationId, result.ConversationId);
        }
    }

    [Fact]
    public async Task GetRecentConversations_Returns200()
    {
        using var client = CreateClient(out var factory);
        using (factory)
        {
            var response = await client.GetAsync("/api/conversations/recent");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task Chat_SavesConversationAutomatically()
    {
        using var client = CreateClient(out var factory);
        using (factory)
        {
            var request = new AiChatRequest("What is Azure?");
            var chatResponse = await client.PostAsJsonAsync("/api/ai-foundry/chat", request);
            Assert.Equal(HttpStatusCode.OK, chatResponse.StatusCode);

            var recentResponse = await client.GetAsync("/api/conversations/recent?count=1");
            var recent = await recentResponse.Content.ReadFromJsonAsync<List<ChatConversation>>();
            Assert.NotNull(recent);
            Assert.NotEmpty(recent);
            Assert.Equal("What is Azure?", recent[0].UserPrompt);
        }
    }
}
