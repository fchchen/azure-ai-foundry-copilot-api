using AzureAiFoundryCopilot.Application.Contracts;
using AzureAiFoundryCopilot.Application.Interfaces;

namespace AzureAiFoundryCopilot.Infrastructure.Services;

public sealed class MockGraphMailService : IGraphMailService
{
    public Task<IReadOnlyList<GraphEmailMessage>> GetUnreadInboxMessagesAsync(
        string? accessToken,
        int top,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        IReadOnlyList<GraphEmailMessage> messages =
        [
            new(
                Id: "mock-mail-1",
                Subject: "Q1 roadmap decision needed",
                FromName: "Avery Jones",
                FromAddress: "avery.jones@contoso.com",
                ReceivedAtUtc: now.AddMinutes(-18),
                Preview: "Can you confirm if we should prioritize Copilot plugin OAuth work this sprint?"
            ),
            new(
                Id: "mock-mail-2",
                Subject: "Customer escalation: tenant provisioning delay",
                FromName: "Nora Kim",
                FromAddress: "nora.kim@contoso.com",
                ReceivedAtUtc: now.AddMinutes(-45),
                Preview: "A finance customer reported repeated provisioning failures in production."
            ),
            new(
                Id: "mock-mail-3",
                Subject: "Prep for architecture review",
                FromName: "Liam Patel",
                FromAddress: "liam.patel@contoso.com",
                ReceivedAtUtc: now.AddHours(-2),
                Preview: "Please share your Azure AI Foundry integration sequence diagram before 3 PM."
            )
        ];

        return Task.FromResult<IReadOnlyList<GraphEmailMessage>>(messages.Take(Math.Max(1, top)).ToList());
    }
}
