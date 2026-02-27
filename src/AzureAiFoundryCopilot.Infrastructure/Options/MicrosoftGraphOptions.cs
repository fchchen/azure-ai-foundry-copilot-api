using System.ComponentModel.DataAnnotations;

namespace AzureAiFoundryCopilot.Infrastructure.Options;

public sealed class MicrosoftGraphOptions
{
    public const string SectionName = "MicrosoftGraph";

    public bool Enabled { get; init; }

    [Required(AllowEmptyStrings = false)]
    [Url]
    public string BaseUrl { get; init; } = "https://graph.microsoft.com";

    [Required(AllowEmptyStrings = false)]
    [RegularExpression("^/.*", ErrorMessage = "UnreadInboxPath must be an absolute path that starts with '/'.")]
    public string UnreadInboxPath { get; init; } = "/v1.0/me/mailFolders/inbox/messages";
}
