using System.ComponentModel.DataAnnotations;

namespace AzureAiFoundryCopilot.Infrastructure.Options;

public sealed class EntraIdOptions
{
    public const string SectionName = "EntraId";

    public bool Enabled { get; init; }

    [Required(AllowEmptyStrings = false)]
    [Url]
    public string Instance { get; init; } = "https://login.microsoftonline.com/";

    [Required(AllowEmptyStrings = false)]
    public string TenantId { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string ClientId { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Audience { get; init; } = string.Empty;
}
