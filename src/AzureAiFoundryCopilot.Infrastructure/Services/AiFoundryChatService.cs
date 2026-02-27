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
                Completion: GenerateMockResponse(request.Prompt),
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

    private static string GenerateMockResponse(string prompt)
    {
        var lower = prompt.ToLowerInvariant();

        if (lower.Contains("azure ai foundry") || lower.Contains("ai foundry"))
            return "Azure AI Foundry is Microsoft's unified platform for building, deploying, and managing AI solutions at enterprise scale. It provides access to leading foundation models—including GPT-4o—along with built-in safety, governance, and observability tooling. You can use it to power chat completions, document analysis, and custom fine-tuned workflows directly within your Microsoft 365 environment.";

        if (lower.Contains("email") || lower.Contains("inbox") || lower.Contains("unread") || lower.Contains("mail"))
            return "I can access your Microsoft 365 inbox via Microsoft Graph to surface unread messages, prioritize by urgency, and suggest next actions. Try the **Summarize unread emails** capability — it identifies action items, highlights escalations, and groups threads by topic so you can triage your inbox in seconds.";

        if (lower.Contains("copilot") || lower.Contains("plugin") || lower.Contains("teams") || lower.Contains("manifest"))
            return "This service exposes a declarative Microsoft 365 Copilot plugin. The manifest chain — `manifest.json` → `declarativeAgent.json` → `ai-plugin.json` — registers the agent's capabilities and OAuth configuration with Microsoft Teams. Once sideloaded, users can invoke it directly in Copilot chat to trigger AI Foundry completions and inbox triage from within their normal workflow.";

        if (lower.Contains("conversation") || lower.Contains("history") || lower.Contains("save") || lower.Contains("recent"))
            return "Conversation history is persisted via the `/api/conversations` endpoints. Each exchange is stored with a unique ID, timestamp, and the full prompt/response pair. In production this uses Azure Blob Storage; in local development it falls back to in-memory storage. You can retrieve any past conversation by ID or list your most recent sessions.";

        if (lower.Contains("draft") || lower.Contains("status update") || lower.Contains("write"))
            return "Here is a draft status update:\n\n**Sprint Status — Current Week**\n\n✅ Completed: Azure AI Foundry chat integration, M365 Copilot plugin manifest chain, conversation persistence layer.\n🔄 In progress: OAuth configuration for Teams sideload, end-to-end integration tests.\n⚠️ Blocked: Awaiting Entra ID app registration approval from the tenant admin.\n\nOverall health: **On track**. No scope changes anticipated this sprint.";

        if (lower.Contains("priority") || lower.Contains("prioritize") || lower.Contains("what should i") || lower.Contains("focus") || lower.Contains("tackle"))
            return "Based on your current context, here are my recommended priorities:\n\n1. **High** — Review the customer escalation in your inbox (tenant provisioning failure — finance customer).\n2. **High** — Confirm Q1 roadmap decision with Avery before sprint planning tomorrow.\n3. **Medium** — Share the Azure AI Foundry integration sequence diagram with Liam by 3 PM.\n4. **Low** — Schedule architecture review follow-up for next week.\n\nWould you like me to draft a response to any of these?";

        if (lower.Contains("help") || lower.Contains("what can you") || lower.Contains("capabilities") || lower.Contains("feature"))
            return "I'm the Azure AI Foundry Copilot assistant. Here's what I can do:\n\n1. **Answer questions** — Ask me anything about Azure AI, Microsoft 365, or your enterprise workflows.\n2. **Summarize your inbox** — I'll pull unread emails via Microsoft Graph and prioritize action items.\n3. **Draft content** — Status updates, meeting agendas, summaries, and more.\n4. **Manage conversations** — I save our exchanges so you can retrieve or review them later.\n5. **Integrate with Teams** — I'm available as a declarative Copilot plugin directly in Microsoft Teams.";

        return $"I've processed your request: \"{prompt.Trim()}\"\n\nAs your Azure AI Foundry Copilot, I can help with enterprise AI tasks, Microsoft 365 inbox triage, and workflow automation. Try asking me to summarize your unread emails, draft a status update, or explain a specific Azure AI capability.";
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
