using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using AzureAiFoundryCopilot.Application.Contracts;
using AzureAiFoundryCopilot.Application.Exceptions;
using AzureAiFoundryCopilot.Application.Interfaces;
using AzureAiFoundryCopilot.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzureAiFoundryCopilot.Infrastructure.Services;

public sealed class GraphMailService : IGraphMailService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly MicrosoftGraphOptions _options;
    private readonly ILogger<GraphMailService> _logger;

    public GraphMailService(
        HttpClient httpClient,
        IOptions<MicrosoftGraphOptions> options,
        ILogger<GraphMailService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<GraphEmailMessage>> GetUnreadInboxMessagesAsync(
        string? accessToken,
        int top,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new AccessTokenRequiredException("A bearer access token is required for Microsoft Graph mailbox access.");

        var boundedTop = Math.Clamp(top, 1, 25);

        var requestUri =
            $"{_options.UnreadInboxPath}?$filter=isRead eq false&$select=id,subject,receivedDateTime,bodyPreview,from&$orderby=receivedDateTime desc&$top={boundedTop}";

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Microsoft Graph unread-mail query failed with status {StatusCode}. Body: {Body}",
                (int)response.StatusCode,
                body);
            throw new InvalidOperationException(
                $"Microsoft Graph unread-mail query failed with status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<GraphMessagesResponse>(stream, JsonOptions, cancellationToken);

        return payload?.Value?.Select(MapMessage).ToList() ?? [];
    }

    private static GraphEmailMessage MapMessage(GraphMessage message) =>
        new(
            Id: message.Id ?? string.Empty,
            Subject: message.Subject ?? "(No subject)",
            FromName: message.From?.EmailAddress?.Name ?? "Unknown sender",
            FromAddress: message.From?.EmailAddress?.Address ?? string.Empty,
            ReceivedAtUtc: message.ReceivedDateTime ?? DateTimeOffset.UtcNow,
            Preview: message.BodyPreview ?? string.Empty
        );

    private sealed class GraphMessagesResponse
    {
        [JsonPropertyName("value")]
        public IReadOnlyList<GraphMessage>? Value { get; init; }
    }

    private sealed class GraphMessage
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("subject")]
        public string? Subject { get; init; }

        [JsonPropertyName("receivedDateTime")]
        public DateTimeOffset? ReceivedDateTime { get; init; }

        [JsonPropertyName("bodyPreview")]
        public string? BodyPreview { get; init; }

        [JsonPropertyName("from")]
        public GraphFrom? From { get; init; }
    }

    private sealed class GraphFrom
    {
        [JsonPropertyName("emailAddress")]
        public GraphEmailAddress? EmailAddress { get; init; }
    }

    private sealed class GraphEmailAddress
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("address")]
        public string? Address { get; init; }
    }
}
