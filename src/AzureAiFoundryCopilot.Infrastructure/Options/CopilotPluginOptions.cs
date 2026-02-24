namespace AzureAiFoundryCopilot.Infrastructure.Options;

public sealed class CopilotPluginOptions
{
    public const string SectionName = "CopilotPlugin";

    public string PluginId { get; init; } = "azure-foundry-assistant";

    public string Name { get; init; } = "Azure Foundry Assistant";

    public string Description { get; init; } = "Copilot plugin for enterprise AI task automation.";

    public string FullDescription { get; init; } = "A Microsoft 365 Copilot plugin that connects to Azure AI Foundry for chat completions, conversation history, and enterprise AI workflows.";

    public string ApiBaseUrl { get; init; } = "https://localhost:7181";

    public string DeveloperName { get; init; } = "Contoso";

    public string WebsiteUrl { get; init; } = "https://github.com/fchchen/azure-ai-foundry-copilot-api";

    public string PrivacyUrl { get; init; } = "https://github.com/fchchen/azure-ai-foundry-copilot-api/blob/main/PRIVACY.md";

    public string TermsOfUseUrl { get; init; } = "https://github.com/fchchen/azure-ai-foundry-copilot-api/blob/main/TERMS.md";

    public string ContactEmail { get; init; } = "admin@contoso.com";

    public string Version { get; init; } = "1.0.0";
}
