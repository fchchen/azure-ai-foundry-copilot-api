using System.ComponentModel.DataAnnotations;

namespace AzureAiFoundryCopilot.Application.Contracts;

public sealed record UnreadEmailSummaryRequest(
    [param: Range(1, 25)] int Top = 5,
    [param: Range(64, 4096)] int MaxTokens = 512,
    [param: Range(0.0, 2.0)] double Temperature = 0.2
);
