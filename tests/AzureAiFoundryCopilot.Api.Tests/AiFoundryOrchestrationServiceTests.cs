using AzureAiFoundryCopilot.Application.Contracts;
using AzureAiFoundryCopilot.Application.Interfaces;
using AzureAiFoundryCopilot.Application.Services;

namespace AzureAiFoundryCopilot.Api.Tests;

public sealed class AiFoundryOrchestrationServiceTests
{
    [Fact]
    public async Task CompleteAndPersistConversationAsync_SavesConversationAndReturnsCompletion()
    {
        var completion = new AiChatResponse(
            Completion: "Hello from model",
            Model: "gpt-test",
            CreatedAtUtc: DateTimeOffset.UtcNow,
            Sources: ["test://source"]);
        var chatService = new StubChatService(completion);
        var graphService = new StubGraphMailService([]);
        var storage = new StubConversationStorageService();

        var sut = new AiFoundryOrchestrationService(chatService, graphService, storage);
        var request = new AiChatRequest("test prompt");

        var result = await sut.CompleteAndPersistConversationAsync(request, CancellationToken.None);

        Assert.Equal(completion, result);
        Assert.Single(storage.SavedConversations);
        Assert.Equal("test prompt", storage.SavedConversations[0].UserPrompt);
        Assert.Equal("Hello from model", storage.SavedConversations[0].AiResponse);
    }

    [Fact]
    public async Task SummarizeUnreadEmailsAsync_WhenNoUnread_ReturnsNoUnreadSummary()
    {
        var chatService = new StubChatService(new AiChatResponse("unused", "unused", DateTimeOffset.UtcNow, ["unused"]));
        var graphService = new StubGraphMailService([]);
        var storage = new StubConversationStorageService();
        var sut = new AiFoundryOrchestrationService(chatService, graphService, storage);

        var result = await sut.SummarizeUnreadEmailsAsync(
            new UnreadEmailSummaryRequest(Top: 5),
            accessToken: "token",
            CancellationToken.None);

        Assert.Equal(0, result.UnreadCount);
        Assert.Equal("No unread inbox messages were found.", result.Summary);
    }

    [Fact]
    public async Task SummarizeUnreadEmailsAsync_WhenUnread_BuildsPromptAndCallsChat()
    {
        var unreadEmails = new[]
        {
            new GraphEmailMessage(
                Id: "1",
                Subject: "Escalation",
                FromName: "Alex",
                FromAddress: "alex@contoso.com",
                ReceivedAtUtc: DateTimeOffset.UtcNow,
                Preview: "Customer issue")
        };

        var completion = new AiChatResponse(
            Completion: "Priority: escalation first.",
            Model: "gpt-test",
            CreatedAtUtc: DateTimeOffset.UtcNow,
            Sources: ["test://source"]);
        var chatService = new StubChatService(completion);
        var graphService = new StubGraphMailService(unreadEmails);
        var storage = new StubConversationStorageService();
        var sut = new AiFoundryOrchestrationService(chatService, graphService, storage);

        var result = await sut.SummarizeUnreadEmailsAsync(
            new UnreadEmailSummaryRequest(Top: 1),
            accessToken: "token",
            CancellationToken.None);

        Assert.Equal(1, result.UnreadCount);
        Assert.Equal("Priority: escalation first.", result.Summary);
        Assert.NotNull(chatService.LastRequest);
        Assert.Contains("Summarize the unread inbox items", chatService.LastRequest!.Prompt);
        Assert.Contains("Escalation", chatService.LastRequest.Prompt);
    }

    private sealed class StubChatService : IAiFoundryChatService
    {
        private readonly AiChatResponse _response;

        public StubChatService(AiChatResponse response)
        {
            _response = response;
        }

        public AiChatRequest? LastRequest { get; private set; }

        public Task<AiChatResponse> CompleteAsync(AiChatRequest request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_response);
        }
    }

    private sealed class StubGraphMailService : IGraphMailService
    {
        private readonly IReadOnlyList<GraphEmailMessage> _emails;

        public StubGraphMailService(IReadOnlyList<GraphEmailMessage> emails)
        {
            _emails = emails;
        }

        public Task<IReadOnlyList<GraphEmailMessage>> GetUnreadInboxMessagesAsync(
            string? accessToken,
            int top,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<GraphEmailMessage>>(_emails.Take(top).ToList());
    }

    private sealed class StubConversationStorageService : IConversationStorageService
    {
        public List<ChatConversation> SavedConversations { get; } = [];

        public Task SaveAsync(ChatConversation conversation, CancellationToken cancellationToken = default)
        {
            SavedConversations.Add(conversation);
            return Task.CompletedTask;
        }

        public Task<ChatConversation?> GetAsync(string conversationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ChatConversation?>(SavedConversations.FirstOrDefault(c => c.ConversationId == conversationId));

        public Task<IReadOnlyList<ChatConversation>> ListRecentAsync(int count = 10, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ChatConversation>>(SavedConversations.Take(count).ToList());
    }
}
