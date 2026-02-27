using System.ComponentModel.DataAnnotations;

namespace AzureAiFoundryCopilot.Infrastructure.Options;

public sealed class CopilotPluginOptions
{
    public const string SectionName = "CopilotPlugin";

    [Required(AllowEmptyStrings = false)]
    public string PluginId { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Name { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Description { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string FullDescription { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [Url]
    public string ApiBaseUrl { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string DeveloperName { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [Url]
    public string WebsiteUrl { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [Url]
    public string PrivacyUrl { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [Url]
    public string TermsOfUseUrl { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [EmailAddress]
    public string ContactEmail { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Version { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Instructions { get; init; } = string.Empty;

    [MinLength(1)]
    public IReadOnlyList<string> ConversationStarters { get; init; } = [];
}
