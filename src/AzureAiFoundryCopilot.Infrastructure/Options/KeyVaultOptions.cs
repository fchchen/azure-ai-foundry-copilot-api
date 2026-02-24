namespace AzureAiFoundryCopilot.Infrastructure.Options;

public sealed class KeyVaultOptions
{
    public const string SectionName = "KeyVault";

    public bool Enabled { get; init; }

    public string VaultUri { get; init; } = string.Empty;
}
