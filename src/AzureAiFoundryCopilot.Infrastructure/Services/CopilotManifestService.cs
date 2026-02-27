using AzureAiFoundryCopilot.Application.Contracts;
using AzureAiFoundryCopilot.Application.Interfaces;
using AzureAiFoundryCopilot.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace AzureAiFoundryCopilot.Infrastructure.Services;

public sealed class CopilotManifestService : ICopilotManifestService
{
    private static readonly IReadOnlyList<PluginFunction> Functions =
    [
        new("sendChatPrompt", "Send a chat prompt to Azure AI Foundry and receive an AI-generated completion."),
        new("summarizeMyUnreadEmails", "Summarize unread Microsoft 365 inbox messages and propose next actions."),
        new("getConversation", "Retrieve a specific conversation by its unique identifier."),
        new("listRecentConversations", "List the most recent conversations, optionally specifying how many to return."),
        new("saveConversation", "Save a conversation record with user prompt and AI response.")
    ];

    private readonly CopilotPluginOptions _options;

    public CopilotManifestService(IOptions<CopilotPluginOptions> options)
    {
        _options = options.Value;
    }

    public CopilotManifestResponse GetManifest() =>
        new(
            PluginId: _options.PluginId,
            Name: _options.Name,
            Description: _options.Description,
            FullDescription: _options.FullDescription,
            ApiBaseUrl: _options.ApiBaseUrl,
            Developer: new DeveloperInfo(
                Name: _options.DeveloperName,
                WebsiteUrl: _options.WebsiteUrl,
                PrivacyUrl: _options.PrivacyUrl,
                TermsOfUseUrl: _options.TermsOfUseUrl
            ),
            DeclarativeAgent: new DeclarativeAgentInfo(
                SchemaVersion: "v1.6",
                Instructions: _options.Instructions,
                ConversationStarters: _options.ConversationStarters
            ),
            ApiPlugin: new ApiPluginInfo(
                SchemaVersion: "v2.4",
                AuthType: "OAuth",
                OpenApiSpecUrl: "openapi.json",
                Functions: Functions
            )
        );
}
