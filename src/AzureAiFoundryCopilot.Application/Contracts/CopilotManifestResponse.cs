namespace AzureAiFoundryCopilot.Application.Contracts;

public sealed record CopilotManifestResponse(
    string PluginId,
    string Name,
    string Description,
    string FullDescription,
    string ApiBaseUrl,
    DeveloperInfo Developer,
    DeclarativeAgentInfo DeclarativeAgent,
    ApiPluginInfo ApiPlugin
);

public sealed record DeveloperInfo(
    string Name,
    string WebsiteUrl,
    string PrivacyUrl,
    string TermsOfUseUrl
);

public sealed record DeclarativeAgentInfo(
    string SchemaVersion,
    string Instructions,
    IReadOnlyList<string> ConversationStarters
);

public sealed record ApiPluginInfo(
    string SchemaVersion,
    string AuthType,
    string OpenApiSpecUrl,
    IReadOnlyList<PluginFunction> Functions
);

public sealed record PluginFunction(
    string Name,
    string Description
);
