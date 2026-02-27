using AzureAiFoundryCopilot.Application.Contracts;

namespace AzureAiFoundryCopilot.Application.Interfaces;

public interface IAiFoundryOrchestrationService
{
    Task<AiChatResponse> CompleteAndPersistConversationAsync(
        AiChatRequest request,
        CancellationToken cancellationToken);

    Task<UnreadEmailSummaryResponse> SummarizeUnreadEmailsAsync(
        UnreadEmailSummaryRequest request,
        string? accessToken,
        CancellationToken cancellationToken);
}
