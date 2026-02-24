using AzureAiFoundryCopilot.Application.Contracts;

namespace AzureAiFoundryCopilot.Application.Interfaces;

public interface IConversationStorageService
{
    Task SaveAsync(ChatConversation conversation, CancellationToken cancellationToken = default);

    Task<ChatConversation?> GetAsync(string conversationId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChatConversation>> ListRecentAsync(int count = 10, CancellationToken cancellationToken = default);
}
