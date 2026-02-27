namespace AzureAiFoundryCopilot.Application.Contracts;

public sealed record UnreadEmailSummaryResponse(
    string Summary,
    int UnreadCount,
    IReadOnlyList<GraphEmailMessage> Emails,
    string Model,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyCollection<string> Sources
);
