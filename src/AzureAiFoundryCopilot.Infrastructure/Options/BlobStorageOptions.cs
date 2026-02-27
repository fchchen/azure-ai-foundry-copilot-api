using System.ComponentModel.DataAnnotations;

namespace AzureAiFoundryCopilot.Infrastructure.Options;

public sealed class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    public bool Enabled { get; init; }

    public string ConnectionString { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string ContainerName { get; init; } = "conversations";
}
