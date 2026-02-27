using System.ComponentModel.DataAnnotations;

namespace AzureAiFoundryCopilot.Infrastructure.Options;

public sealed class AzureAiFoundryOptions
{
    public const string SectionName = "AzureAiFoundry";

    [Required(AllowEmptyStrings = false)]
    [Url]
    public string Endpoint { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Deployment { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public string ApiKeySecretName { get; init; } = "AzureAiFoundry--ApiKey";

    public bool UseMockResponses { get; init; } = true;
}
