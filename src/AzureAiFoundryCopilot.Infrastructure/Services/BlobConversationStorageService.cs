using System.Text.Json;
using Azure.Storage.Blobs;
using AzureAiFoundryCopilot.Application.Contracts;
using AzureAiFoundryCopilot.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AzureAiFoundryCopilot.Infrastructure.Services;

public sealed class BlobConversationStorageService : IConversationStorageService
{
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
        await blobClient.UploadAsync(new BinaryData(json), overwrite: true, cancellationToken);
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

        var conversations = new List<ChatConversation>();

        await foreach (var blob in _containerClient.GetBlobsAsync(cancellationToken: cancellationToken))
        {
            var blobClient = _containerClient.GetBlobClient(blob.Name);
            var response = await blobClient.DownloadContentAsync(cancellationToken);
            var conversation = response.Value.Content.ToObjectFromJson<ChatConversation>(JsonOptions);

            if (conversation is not null)
                conversations.Add(conversation);
        }

        return conversations
            .OrderByDescending(c => c.CreatedAtUtc)
            .Take(count)
            .ToList();
    }
}
