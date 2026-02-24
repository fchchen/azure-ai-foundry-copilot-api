namespace AzureAiFoundryCopilot.Application.Contracts;

public sealed record AiChatResponse(
    string Completion,
    string Model,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyCollection<string> Sources
);
