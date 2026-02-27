namespace AzureAiFoundryCopilot.Application.Contracts;

public sealed record GraphEmailMessage(
    string Id,
    string Subject,
    string FromName,
    string FromAddress,
    DateTimeOffset ReceivedAtUtc,
    string Preview
);
