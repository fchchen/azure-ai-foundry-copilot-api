using System.Collections.Concurrent;
using AzureAiFoundryCopilot.Application.Contracts;
using AzureAiFoundryCopilot.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AzureAiFoundryCopilot.Infrastructure.Services;

public sealed class InMemoryConversationStorageService : IConversationStorageService
{
    private readonly ConcurrentDictionary<string, ChatConversation> _conversations = new();
    private readonly ILogger<InMemoryConversationStorageService> _logger;

    public InMemoryConversationStorageService(ILogger<InMemoryConversationStorageService> logger)
    {
        _logger = logger;
    }

    public Task SaveAsync(ChatConversation conversation, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Saving conversation {ConversationId} to in-memory store.", conversation.ConversationId);

        _conversations[conversation.ConversationId] = conversation;
        return Task.CompletedTask;
    }

    public Task<ChatConversation?> GetAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving conversation {ConversationId} from in-memory store.", conversationId);

        _conversations.TryGetValue(conversationId, out var conversation);
        return Task.FromResult(conversation);
    }

    public Task<IReadOnlyList<ChatConversation>> ListRecentAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listing recent {Count} conversations from in-memory store.", count);

        IReadOnlyList<ChatConversation> recent = _conversations.Values
            .OrderByDescending(c => c.CreatedAtUtc)
            .Take(count)
            .ToList();

        return Task.FromResult(recent);
    }
}
