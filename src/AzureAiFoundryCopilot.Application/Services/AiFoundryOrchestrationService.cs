using System.Text;
using AzureAiFoundryCopilot.Application.Contracts;
using AzureAiFoundryCopilot.Application.Interfaces;

namespace AzureAiFoundryCopilot.Application.Services;

public sealed class AiFoundryOrchestrationService : IAiFoundryOrchestrationService
{
    private readonly IAiFoundryChatService _chatService;
    private readonly IGraphMailService _graphMailService;
    private readonly IConversationStorageService _conversationStorage;

    public AiFoundryOrchestrationService(
        IAiFoundryChatService chatService,
        IGraphMailService graphMailService,
        IConversationStorageService conversationStorage)
    {
        _chatService = chatService;
        _graphMailService = graphMailService;
        _conversationStorage = conversationStorage;
    }

    public async Task<AiChatResponse> CompleteAndPersistConversationAsync(
        AiChatRequest request,
        CancellationToken cancellationToken)
    {
        var completion = await _chatService.CompleteAsync(request, cancellationToken);
        var conversation = new ChatConversation(
            ConversationId: Guid.NewGuid().ToString("N"),
            UserPrompt: request.Prompt,
            AiResponse: completion.Completion,
            CreatedAtUtc: completion.CreatedAtUtc);

        await _conversationStorage.SaveAsync(conversation, cancellationToken);
        return completion;
    }

    public async Task<UnreadEmailSummaryResponse> SummarizeUnreadEmailsAsync(
        UnreadEmailSummaryRequest request,
        string? accessToken,
        CancellationToken cancellationToken)
    {
        var unreadEmails = await _graphMailService.GetUnreadInboxMessagesAsync(accessToken, request.Top, cancellationToken);
        if (unreadEmails.Count == 0)
        {
            return new UnreadEmailSummaryResponse(
                Summary: "No unread inbox messages were found.",
                UnreadCount: 0,
                Emails: [],
                Model: "none",
                CreatedAtUtc: DateTimeOffset.UtcNow,
                Sources: ["graph://inbox"]);
        }

        var prompt = BuildUnreadEmailSummaryPrompt(unreadEmails);
        var completion = await _chatService.CompleteAsync(
            new AiChatRequest(prompt, request.MaxTokens, request.Temperature),
            cancellationToken);

        return new UnreadEmailSummaryResponse(
            Summary: completion.Completion,
            UnreadCount: unreadEmails.Count,
            Emails: unreadEmails,
            Model: completion.Model,
            CreatedAtUtc: completion.CreatedAtUtc,
            Sources: completion.Sources);
    }

    private static string BuildUnreadEmailSummaryPrompt(IReadOnlyList<GraphEmailMessage> unreadEmails)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Summarize the unread inbox items for an enterprise user.");
        builder.AppendLine("Return:");
        builder.AppendLine("1) a short executive summary");
        builder.AppendLine("2) top priorities with rationale");
        builder.AppendLine("3) suggested next actions");
        builder.AppendLine();
        builder.AppendLine("Unread messages:");

        for (var i = 0; i < unreadEmails.Count; i++)
        {
            var email = unreadEmails[i];
            builder.AppendLine(
                $"{i + 1}. Subject: {email.Subject} | From: {email.FromName} <{email.FromAddress}> | Received: {email.ReceivedAtUtc:u}");
            builder.AppendLine($"   Preview: {email.Preview}");
        }

        return builder.ToString();
    }
}
