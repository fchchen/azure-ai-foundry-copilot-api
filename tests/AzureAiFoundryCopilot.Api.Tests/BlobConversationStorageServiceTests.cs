using Azure.Storage.Blobs.Models;
using AzureAiFoundryCopilot.Infrastructure.Services;

namespace AzureAiFoundryCopilot.Api.Tests;

public sealed class BlobConversationStorageServiceTests
{
    [Fact]
    public void SelectRecentBlobNames_UsesMetadataTimestampWhenAvailable()
    {
        var older = BlobsModelFactory.BlobItem(
            name: "older.json",
            metadata: new Dictionary<string, string>
            {
                [BlobConversationStorageService.CreatedAtMetadataKey] = DateTimeOffset.UtcNow.AddHours(-2).ToString("O")
            });
        var newer = BlobsModelFactory.BlobItem(
            name: "newer.json",
            metadata: new Dictionary<string, string>
            {
                [BlobConversationStorageService.CreatedAtMetadataKey] = DateTimeOffset.UtcNow.ToString("O")
            });

        var result = BlobConversationStorageService.SelectRecentBlobNames([older, newer], count: 1);

        Assert.Single(result);
        Assert.Equal("newer.json", result[0]);
    }

    [Fact]
    public void ResolveSortTimestamp_FallsBackToMinValueWhenMetadataAndLastModifiedMissing()
    {
        var blob = BlobsModelFactory.BlobItem(
            name: "no-meta.json",
            metadata: new Dictionary<string, string>());

        var result = BlobConversationStorageService.ResolveSortTimestamp(blob);

        Assert.Equal(DateTimeOffset.MinValue, result);
    }
}
