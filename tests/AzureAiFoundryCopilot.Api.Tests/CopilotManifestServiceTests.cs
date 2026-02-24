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
            FullDescription = "A full description of the plugin.",
            ApiBaseUrl = "https://api.contoso.com",
            DeveloperName = "Contoso Dev",
            WebsiteUrl = "https://contoso.com",
            PrivacyUrl = "https://contoso.com/privacy",
            TermsOfUseUrl = "https://contoso.com/terms",
            ContactEmail = "dev@contoso.com",
            Version = "2.0.0"
        });
        var service = new CopilotManifestService(options);

        var manifest = service.GetManifest();

        Assert.Equal("contoso-mail-assistant", manifest.PluginId);
        Assert.Equal("Contoso Mail Assistant", manifest.Name);
        Assert.Equal("https://api.contoso.com", manifest.ApiBaseUrl);
        Assert.Equal("A full description of the plugin.", manifest.FullDescription);
    }

    [Fact]
    public void GetManifest_ReturnsDeveloperInfo()
    {
        var options = Options.Create(new CopilotPluginOptions
        {
            DeveloperName = "Contoso Dev",
            WebsiteUrl = "https://contoso.com",
            PrivacyUrl = "https://contoso.com/privacy",
            TermsOfUseUrl = "https://contoso.com/terms"
        });
        var service = new CopilotManifestService(options);

        var manifest = service.GetManifest();

        Assert.Equal("Contoso Dev", manifest.Developer.Name);
        Assert.Equal("https://contoso.com", manifest.Developer.WebsiteUrl);
        Assert.Equal("https://contoso.com/privacy", manifest.Developer.PrivacyUrl);
        Assert.Equal("https://contoso.com/terms", manifest.Developer.TermsOfUseUrl);
    }

    [Fact]
    public void GetManifest_ReturnsDeclarativeAgentInfo()
    {
        var options = Options.Create(new CopilotPluginOptions());
        var service = new CopilotManifestService(options);

        var manifest = service.GetManifest();

        Assert.Equal("v1.6", manifest.DeclarativeAgent.SchemaVersion);
        Assert.NotEmpty(manifest.DeclarativeAgent.Instructions);
        Assert.Equal(3, manifest.DeclarativeAgent.ConversationStarters.Count);
    }

    [Fact]
    public void GetManifest_ReturnsApiPluginInfo()
    {
        var options = Options.Create(new CopilotPluginOptions());
        var service = new CopilotManifestService(options);

        var manifest = service.GetManifest();

        Assert.Equal("v2.4", manifest.ApiPlugin.SchemaVersion);
        Assert.Equal("None", manifest.ApiPlugin.AuthType);
        Assert.Equal("openapi.json", manifest.ApiPlugin.OpenApiSpecUrl);
        Assert.Equal(4, manifest.ApiPlugin.Functions.Count);
    }

    [Fact]
    public void GetManifest_FunctionsMatchExpectedOperationIds()
    {
        var options = Options.Create(new CopilotPluginOptions());
        var service = new CopilotManifestService(options);

        var manifest = service.GetManifest();
        var names = manifest.ApiPlugin.Functions.Select(f => f.Name).ToList();

        Assert.Contains("sendChatPrompt", names);
        Assert.Contains("getConversation", names);
        Assert.Contains("listRecentConversations", names);
        Assert.Contains("saveConversation", names);
    }
}
