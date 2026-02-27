using System.Net;
using System.Net.Http.Json;
using AzureAiFoundryCopilot.Application.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace AzureAiFoundryCopilot.Api.Tests;

public sealed class AuthIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_AllowsAnonymous()
    {
        var response = await _client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CopilotManifest_AllowsAnonymous()
    {
        var response = await _client.GetAsync("/api/copilot/manifest");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ChatEndpoint_WithMockAuth_Returns200()
    {
        var request = new AiChatRequest("Test prompt");
        var response = await _client.PostAsJsonAsync("/api/ai-foundry/chat", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ConversationsEndpoint_WithMockAuth_Returns200()
    {
        var response = await _client.GetAsync("/api/conversations/recent");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ChatEndpoint_WhenAuthenticationFails_Returns401()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureTestServices(services =>
                {
                    services.AddAuthentication("Reject")
                        .AddScheme<AuthenticationSchemeOptions, RejectAuthHandler>("Reject", _ => { });

                    services.PostConfigure<AuthenticationOptions>(options =>
                    {
                        options.DefaultAuthenticateScheme = "Reject";
                        options.DefaultChallengeScheme = "Reject";
                    });
                });
            });

        using var client = factory.CreateClient();
        var request = new AiChatRequest("Test prompt");
        var response = await client.PostAsJsonAsync("/api/ai-foundry/chat", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed class RejectAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public RejectAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
            Task.FromResult(AuthenticateResult.Fail("Unauthorized for test validation."));
    }
}
