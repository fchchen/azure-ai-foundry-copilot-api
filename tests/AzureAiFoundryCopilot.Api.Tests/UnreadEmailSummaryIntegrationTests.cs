using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AzureAiFoundryCopilot.Application.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AzureAiFoundryCopilot.Api.Tests;

public sealed class UnreadEmailSummaryIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public UnreadEmailSummaryIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UnreadEmailSummary_Returns200WithUnreadEmails()
    {
        var request = new UnreadEmailSummaryRequest(Top: 3);

        var response = await _client.PostAsJsonAsync("/api/ai-foundry/unread-email-summary", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(payload.GetProperty("unreadCount").GetInt32() > 0);
        Assert.True(payload.GetProperty("emails").GetArrayLength() > 0);
        Assert.False(string.IsNullOrWhiteSpace(payload.GetProperty("summary").GetString()));
    }
}
