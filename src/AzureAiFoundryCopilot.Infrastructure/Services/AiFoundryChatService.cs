using System.ClientModel;
using Azure.AI.OpenAI;
using AzureAiFoundryCopilot.Application.Contracts;
using AzureAiFoundryCopilot.Application.Interfaces;
using AzureAiFoundryCopilot.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace AzureAiFoundryCopilot.Infrastructure.Services;

public sealed class AiFoundryChatService : IAiFoundryChatService
{
    private readonly AzureAiFoundryOptions _options;
    private readonly ISecretService _secretService;
    private readonly ILogger<AiFoundryChatService> _logger;
    private readonly SemaphoreSlim _clientInitLock = new(1, 1);
    private ChatClient? _chatClient;

    public AiFoundryChatService(
        IOptions<AzureAiFoundryOptions> options,
        ISecretService secretService,
        ILogger<AiFoundryChatService> logger)
    {
        _options = options.Value;
        _secretService = secretService;
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

        var apiKey = await ResolveApiKeyAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(_options.Endpoint) ||
            string.IsNullOrWhiteSpace(_options.Deployment) ||
            string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Azure AI Foundry configuration is incomplete.");
        }

        var chatClient = await GetChatClientAsync(apiKey, cancellationToken);

        var messages = new ChatMessage[]
        {
            new SystemChatMessage("You are an enterprise copilot for Microsoft 365 workflows."),
            new UserChatMessage(request.Prompt)
        };

        var chatOptions = new ChatCompletionOptions
        {
            MaxOutputTokenCount = request.MaxTokens,
            Temperature = (float)request.Temperature
        };

        ChatCompletion completion = await chatClient.CompleteChatAsync(messages, chatOptions, cancellationToken);

        var content = completion.Content[0].Text;
        if (string.IsNullOrWhiteSpace(content))
        {
            _logger.LogWarning("Azure AI Foundry response did not include content.");
            throw new InvalidOperationException("Azure AI Foundry response did not include content.");
        }

        return new AiChatResponse(
            Completion: content,
            Model: completion.Model ?? _options.Deployment,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            Sources: [$"azure-ai-foundry://{_options.Deployment}"]
        );
    }

    private async Task<ChatClient> GetChatClientAsync(string apiKey, CancellationToken cancellationToken)
    {
        if (_chatClient is not null)
            return _chatClient;

        await _clientInitLock.WaitAsync(cancellationToken);
        try
        {
            if (_chatClient is not null)
                return _chatClient;

            var azureClient = new AzureOpenAIClient(
                new Uri(_options.Endpoint),
                new ApiKeyCredential(apiKey));
            _chatClient = azureClient.GetChatClient(_options.Deployment);
            return _chatClient;
        }
        finally
        {
            _clientInitLock.Release();
        }
    }

    private async Task<string> ResolveApiKeyAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            return _options.ApiKey;

        if (string.IsNullOrWhiteSpace(_options.ApiKeySecretName))
            return string.Empty;

        var secretValue = await _secretService.GetSecretAsync(_options.ApiKeySecretName, cancellationToken);
        if (!string.IsNullOrWhiteSpace(secretValue))
            return secretValue;

        _logger.LogWarning(
            "Azure AI Foundry API key was not configured directly and secret {SecretName} was not found.",
            _options.ApiKeySecretName);
        return string.Empty;
    }
}
