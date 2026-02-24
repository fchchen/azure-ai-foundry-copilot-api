namespace AzureAiFoundryCopilot.Application.Contracts;

public sealed record CopilotManifestResponse(
    string PluginId,
    string Name,
    string Description,
    string ApiBaseUrl,
    IReadOnlyCollection<string> SupportedScopes
);
