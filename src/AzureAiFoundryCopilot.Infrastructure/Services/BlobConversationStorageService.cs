using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using AzureAiFoundryCopilot.Application.Contracts;
using AzureAiFoundryCopilot.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AzureAiFoundryCopilot.Infrastructure.Services;

public sealed class BlobConversationStorageService : IConversationStorageService
{
    internal const string CreatedAtMetadataKey = "createdatutc";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<BlobConversationStorageService> _logger;

    public BlobConversationStorageService(BlobContainerClient containerClient, ILogger<BlobConversationStorageService> logger)
    {
        _containerClient = containerClient;
        _logger = logger;
    }

    public async Task SaveAsync(ChatConversation conversation, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Saving conversation {ConversationId} to blob storage.", conversation.ConversationId);

        var blobClient = _containerClient.GetBlobClient($"{conversation.ConversationId}.json");
        var json = JsonSerializer.SerializeToUtf8Bytes(conversation, JsonOptions);
        var uploadOptions = new BlobUploadOptions
        {
            Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [CreatedAtMetadataKey] = conversation.CreatedAtUtc.UtcDateTime.ToString("O")
            }
        };
        await blobClient.UploadAsync(new BinaryData(json), uploadOptions, cancellationToken);
    }

    public async Task<ChatConversation?> GetAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving conversation {ConversationId} from blob storage.", conversationId);

        var blobClient = _containerClient.GetBlobClient($"{conversationId}.json");

        if (!await blobClient.ExistsAsync(cancellationToken))
            return null;

        var response = await blobClient.DownloadContentAsync(cancellationToken);
        return response.Value.Content.ToObjectFromJson<ChatConversation>(JsonOptions);
    }

    public async Task<IReadOnlyList<ChatConversation>> ListRecentAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listing recent {Count} conversations from blob storage.", count);
        if (count <= 0)
            return [];

        var blobs = new List<BlobItem>();
        await foreach (var blob in _containerClient.GetBlobsAsync(
            traits: BlobTraits.Metadata,
            states: BlobStates.None,
            prefix: null,
            cancellationToken: cancellationToken))
        {
            blobs.Add(blob);
        }

        var recentBlobNames = SelectRecentBlobNames(blobs, count);
        var conversations = new List<ChatConversation>(recentBlobNames.Count);
        foreach (var blobName in recentBlobNames)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            var response = await blobClient.DownloadContentAsync(cancellationToken);
            var conversation = response.Value.Content.ToObjectFromJson<ChatConversation>(JsonOptions);
            if (conversation is not null)
                conversations.Add(conversation);
        }

        return conversations;
    }

    internal static IReadOnlyList<string> SelectRecentBlobNames(IEnumerable<BlobItem> blobs, int count)
    {
        return blobs
            .OrderByDescending(ResolveSortTimestamp)
            .Take(count)
            .Select(blob => blob.Name)
            .ToList();
    }

    internal static DateTimeOffset ResolveSortTimestamp(BlobItem blob)
    {
        if (blob.Metadata is not null &&
            blob.Metadata.TryGetValue(CreatedAtMetadataKey, out var createdAtRaw) &&
            DateTimeOffset.TryParse(createdAtRaw, out var createdAt))
        {
            return createdAt;
        }

        return blob.Properties?.LastModified ?? DateTimeOffset.MinValue;
    }
}
