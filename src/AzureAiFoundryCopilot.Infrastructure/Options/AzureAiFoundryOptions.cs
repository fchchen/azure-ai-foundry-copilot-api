namespace AzureAiFoundryCopilot.Infrastructure.Options;

public sealed class AzureAiFoundryOptions
{
    public const string SectionName = "AzureAiFoundry";

    public string Endpoint { get; init; } = string.Empty;

    public string Deployment { get; init; } = string.Empty;

    public string ApiVersion { get; init; } = "2024-10-21";

    public string ApiKey { get; init; } = string.Empty;

    public bool UseMockResponses { get; init; } = true;
}
