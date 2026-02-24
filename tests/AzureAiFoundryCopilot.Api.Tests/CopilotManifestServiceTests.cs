using AzureAiFoundryCopilot.Infrastructure.Options;
using AzureAiFoundryCopilot.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace AzureAiFoundryCopilot.Api.Tests;

public sealed class CopilotManifestServiceTests
{
    [Fact]
    public void GetManifest_ReturnsConfiguredValues()
    {
        var options = Options.Create(new CopilotPluginOptions
        {
            PluginId = "contoso-mail-assistant",
            Name = "Contoso Mail Assistant",
            Description = "Prioritizes messages for support managers.",
            ApiBaseUrl = "https://api.contoso.com",
            SupportedScopes = ["Mail.Read", "Mail.Send"]
        });
        var service = new CopilotManifestService(options);

        var manifest = service.GetManifest();

        Assert.Equal("contoso-mail-assistant", manifest.PluginId);
        Assert.Equal("Contoso Mail Assistant", manifest.Name);
        Assert.Equal("https://api.contoso.com", manifest.ApiBaseUrl);
        Assert.Contains("Mail.Read", manifest.SupportedScopes);
    }
}
