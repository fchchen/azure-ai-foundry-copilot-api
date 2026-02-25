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
    private readonly ILogger<AiFoundryChatService> _logger;

    public AiFoundryChatService(IOptions<AzureAiFoundryOptions> options, ILogger<AiFoundryChatService> logger)
    {
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

        var azureClient = new AzureOpenAIClient(
            new Uri(_options.Endpoint),
            new ApiKeyCredential(_options.ApiKey));

        var chatClient = azureClient.GetChatClient(_options.Deployment);

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
}
