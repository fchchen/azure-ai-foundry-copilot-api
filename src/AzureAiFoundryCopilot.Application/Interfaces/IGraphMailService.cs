using AzureAiFoundryCopilot.Application.Contracts;

namespace AzureAiFoundryCopilot.Application.Interfaces;

public interface IGraphMailService
{
    Task<IReadOnlyList<GraphEmailMessage>> GetUnreadInboxMessagesAsync(
        string? accessToken,
        int top,
        CancellationToken cancellationToken);
}
