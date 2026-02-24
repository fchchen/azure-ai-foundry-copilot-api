using AzureAiFoundryCopilot.Application.Contracts;
using AzureAiFoundryCopilot.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace AzureAiFoundryCopilot.Api.Tests;

public sealed class InMemoryConversationStorageServiceTests
{
    private readonly InMemoryConversationStorageService _service = new(NullLogger<InMemoryConversationStorageService>.Instance);

    [Fact]
    public async Task GetAsync_WhenNotStored_ReturnsNull()
    {
        var result = await _service.GetAsync("nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAsync_ThenGet_ReturnsStoredConversation()
    {
        var conversation = new ChatConversation("conv-1", "Hello", "Hi there!", DateTimeOffset.UtcNow);

        await _service.SaveAsync(conversation);
        var result = await _service.GetAsync("conv-1");

        Assert.NotNull(result);
        Assert.Equal("conv-1", result.ConversationId);
        Assert.Equal("Hello", result.UserPrompt);
        Assert.Equal("Hi there!", result.AiResponse);
    }

    [Fact]
    public async Task SaveAsync_OverwritesExisting()
    {
        var original = new ChatConversation("conv-1", "Hello", "Hi", DateTimeOffset.UtcNow);
        var updated = new ChatConversation("conv-1", "Updated", "New response", DateTimeOffset.UtcNow);

        await _service.SaveAsync(original);
        await _service.SaveAsync(updated);
        var result = await _service.GetAsync("conv-1");

        Assert.NotNull(result);
        Assert.Equal("Updated", result.UserPrompt);
    }

    [Fact]
    public async Task ListRecentAsync_ReturnsOrderedByMostRecent()
    {
        var older = new ChatConversation("conv-1", "First", "Response 1", DateTimeOffset.UtcNow.AddMinutes(-10));
        var newer = new ChatConversation("conv-2", "Second", "Response 2", DateTimeOffset.UtcNow);

        await _service.SaveAsync(older);
        await _service.SaveAsync(newer);

        var result = await _service.ListRecentAsync(10);

        Assert.Equal(2, result.Count);
        Assert.Equal("conv-2", result[0].ConversationId);
        Assert.Equal("conv-1", result[1].ConversationId);
    }

    [Fact]
    public async Task ListRecentAsync_RespectsCountLimit()
    {
        for (var i = 0; i < 5; i++)
        {
            await _service.SaveAsync(new ChatConversation($"conv-{i}", $"Prompt {i}", $"Response {i}", DateTimeOffset.UtcNow.AddMinutes(i)));
        }

        var result = await _service.ListRecentAsync(3);

        Assert.Equal(3, result.Count);
    }
}
