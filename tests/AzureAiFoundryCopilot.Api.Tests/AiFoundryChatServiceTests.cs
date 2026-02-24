using AzureAiFoundryCopilot.Application.Contracts;
using AzureAiFoundryCopilot.Infrastructure.Options;
using AzureAiFoundryCopilot.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AzureAiFoundryCopilot.Api.Tests;

public sealed class AiFoundryChatServiceTests
{
    [Fact]
    public async Task CompleteAsync_WhenMockEnabled_ReturnsMockResponse()
    {
        var options = Options.Create(new AzureAiFoundryOptions { UseMockResponses = true });
        using var httpClient = new HttpClient();
        var service = new AiFoundryChatService(httpClient, options, NullLogger<AiFoundryChatService>.Instance);

        var result = await service.CompleteAsync(
            new AiChatRequest("Draft a status update for the release."),
            CancellationToken.None);

        Assert.StartsWith("[Mock] Processed prompt:", result.Completion);
        Assert.Equal("mock-gpt", result.Model);
        Assert.Contains("mock://ai-foundry", result.Sources);
    }

    [Fact]
    public async Task CompleteAsync_WhenNotMockAndMissingConfig_ThrowsInvalidOperation()
    {
        var options = Options.Create(new AzureAiFoundryOptions { UseMockResponses = false });
        using var httpClient = new HttpClient();
        var service = new AiFoundryChatService(httpClient, options, NullLogger<AiFoundryChatService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CompleteAsync(
                new AiChatRequest("Hello"),
                CancellationToken.None));
    }
}
