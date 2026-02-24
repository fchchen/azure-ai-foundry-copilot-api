using AzureAiFoundryCopilot.Application.Contracts;

namespace AzureAiFoundryCopilot.Application.Interfaces;

public interface IAiFoundryChatService
{
    Task<AiChatResponse> CompleteAsync(AiChatRequest request, CancellationToken cancellationToken);
}
