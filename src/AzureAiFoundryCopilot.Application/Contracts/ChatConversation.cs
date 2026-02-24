namespace AzureAiFoundryCopilot.Application.Contracts;

public sealed record ChatConversation(
    string ConversationId,
    string UserPrompt,
    string AiResponse,
    DateTimeOffset CreatedAtUtc
);
