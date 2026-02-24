using System.Net.Http.Json;
using System.Text.Json;
using AzureAiFoundryCopilot.Application.Contracts;
using AzureAiFoundryCopilot.Application.Interfaces;
using AzureAiFoundryCopilot.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzureAiFoundryCopilot.Infrastructure.Services;

public sealed class AiFoundryChatService : IAiFoundryChatService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly AzureAiFoundryOptions _options;
    private readonly ILogger<AiFoundryChatService> _logger;

    public AiFoundryChatService(HttpClient httpClient, IOptions<AzureAiFoundryOptions> options, ILogger<AiFoundryChatService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AiChatResponse> CompleteAsync(AiChatRequest request, CancellationToken cancellationToken)
    {
        if (_options.UseMockResponses)
        {
            _logger.LogInformation("Mock mode enabled — returning synthetic response for prompt.");
            return new AiChatResponse(
                Completion: $"[Mock] Processed prompt: {request.Prompt}",
                Model: "mock-gpt",
                CreatedAtUtc: DateTimeOffset.UtcNow,
                Sources: ["mock://ai-foundry"]
            );
        }

        if (string.IsNullOrWhiteSpace(_options.Endpoint) ||
            string.IsNullOrWhiteSpace(_options.Deployment) ||
            string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Azure AI Foundry configuration is incomplete.");
        }

        var requestBody = new
        {
            messages = new object[]
            {
                new { role = "system", content = "You are an enterprise copilot for Microsoft 365 workflows." },
                new { role = "user", content = request.Prompt }
            },
            max_tokens = request.MaxTokens,
            temperature = request.Temperature
        };

        var endpoint =
            $"{_options.Endpoint.TrimEnd('/')}/openai/deployments/{_options.Deployment}/chat/completions?api-version={_options.ApiVersion}";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(requestBody, options: JsonOptions)
        };
        httpRequest.Headers.Add("api-key", _options.ApiKey);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Azure AI Foundry call failed with status {StatusCode}: {Body}", (int)response.StatusCode, body);
            throw new InvalidOperationException($"Azure AI Foundry call failed: {(int)response.StatusCode} {body}");
        }

        var payload = await response.Content.ReadFromJsonAsync<FoundryChatResponse>(JsonOptions, cancellationToken);
        var completion = payload?.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(completion))
        {
            _logger.LogWarning("Azure AI Foundry response did not include content.");
            throw new InvalidOperationException("Azure AI Foundry response did not include content.");
        }

        return new AiChatResponse(
            Completion: completion,
            Model: payload?.Model ?? _options.Deployment,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            Sources: [$"azure-ai-foundry://{_options.Deployment}"]
        );
    }

    private sealed class FoundryChatResponse
    {
        public string? Model { get; init; }

        public Choice[]? Choices { get; init; }
    }

    private sealed class Choice
    {
        public Message? Message { get; init; }
    }

    private sealed class Message
    {
        public string? Content { get; init; }
    }
}
