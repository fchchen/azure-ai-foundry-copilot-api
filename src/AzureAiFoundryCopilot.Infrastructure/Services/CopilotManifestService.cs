using AzureAiFoundryCopilot.Application.Contracts;
using AzureAiFoundryCopilot.Application.Interfaces;
using AzureAiFoundryCopilot.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace AzureAiFoundryCopilot.Infrastructure.Services;

public sealed class CopilotManifestService : ICopilotManifestService
{
    private readonly CopilotPluginOptions _options;

    public CopilotManifestService(IOptions<CopilotPluginOptions> options)
    {
        _options = options.Value;
    }

    public CopilotManifestResponse GetManifest() =>
        new(
            PluginId: _options.PluginId,
            Name: _options.Name,
            Description: _options.Description,
            ApiBaseUrl: _options.ApiBaseUrl,
            SupportedScopes: _options.SupportedScopes
        );
}
