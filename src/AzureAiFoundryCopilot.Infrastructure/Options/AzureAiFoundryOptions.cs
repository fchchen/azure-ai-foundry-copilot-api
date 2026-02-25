namespace AzureAiFoundryCopilot.Infrastructure.Options;

public sealed class AzureAiFoundryOptions
{
    public const string SectionName = "AzureAiFoundry";

    public string Endpoint { get; init; } = string.Empty;

    public string Deployment { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public bool UseMockResponses { get; init; } = true;
}
