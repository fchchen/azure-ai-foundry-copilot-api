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
        new("getConversation", "Retrieve a specific conversation by its unique identifier."),
        new("listRecentConversations", "List the most recent conversations, optionally specifying how many to return."),
        new("saveConversation", "Save a conversation record with user prompt and AI response.")
    ];

    private static readonly string Instructions =
        """
        You are the Azure Foundry Assistant. You help users interact with Azure AI Foundry services. You have four capabilities:

        1. **Send a chat prompt** — Use `sendChatPrompt` when the user asks a question or wants an AI-generated response.
        2. **Get a conversation** — Use `getConversation` when the user wants to retrieve a specific past conversation by its ID.
        3. **List recent conversations** — Use `listRecentConversations` when the user wants to see their recent conversation history.
        4. **Save a conversation** — Use `saveConversation` when the user explicitly asks to save or bookmark a conversation record.

        Always prefer `sendChatPrompt` for general questions. Only use conversation operations when the user specifically asks about history or saving.
        """;

    private static readonly IReadOnlyList<string> ConversationStarters =
    [
        "What are the key benefits of Azure AI Foundry?",
        "Show me my recent conversations",
        "Look up conversation abc123"
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
                Instructions: Instructions,
                ConversationStarters: ConversationStarters
            ),
            ApiPlugin: new ApiPluginInfo(
                SchemaVersion: "v2.4",
                AuthType: "None",
                OpenApiSpecUrl: "openapi.json",
                Functions: Functions
            )
        );
}
