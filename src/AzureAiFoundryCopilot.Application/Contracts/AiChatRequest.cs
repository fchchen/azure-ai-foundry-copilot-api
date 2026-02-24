using System.ComponentModel.DataAnnotations;

namespace AzureAiFoundryCopilot.Application.Contracts;

public sealed record AiChatRequest(
    [param: Required(AllowEmptyStrings = false)] string Prompt,
    [param: Range(1, 4096)] int MaxTokens = 512,
    [param: Range(0.0, 2.0)] double Temperature = 0.2
);
