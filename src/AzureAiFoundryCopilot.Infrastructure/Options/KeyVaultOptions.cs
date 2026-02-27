using System.ComponentModel.DataAnnotations;

namespace AzureAiFoundryCopilot.Infrastructure.Options;

public sealed class KeyVaultOptions
{
    public const string SectionName = "KeyVault";

    public bool Enabled { get; init; }

    [Required(AllowEmptyStrings = false)]
    [Url]
    public string VaultUri { get; init; } = string.Empty;
}
