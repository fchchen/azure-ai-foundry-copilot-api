namespace AzureAiFoundryCopilot.Infrastructure.Options;

public sealed class CopilotPluginOptions
{
    public const string SectionName = "CopilotPlugin";

    public string PluginId { get; init; } = "azure-foundry-assistant";

    public string Name { get; init; } = "Azure Foundry Assistant";

    public string Description { get; init; } = "Copilot plugin for enterprise AI task automation.";

    public string ApiBaseUrl { get; init; } = "https://localhost:7181";

    public string[] SupportedScopes { get; init; } = ["User.Read", "Mail.Read"];
}
