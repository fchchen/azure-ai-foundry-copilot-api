using AzureAiFoundryCopilot.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace AzureAiFoundryCopilot.Api.Tests;

public sealed class InMemorySecretServiceTests
{
    private readonly InMemorySecretService _service = new(NullLogger<InMemorySecretService>.Instance);

    [Fact]
    public async Task GetSecretAsync_WhenNotSet_ReturnsNull()
    {
        var result = await _service.GetSecretAsync("missing-key");

        Assert.Null(result);
    }

    [Fact]
    public async Task SetSecretAsync_ThenGet_ReturnsStoredValue()
    {
        await _service.SetSecretAsync("api-key", "super-secret-123");

        var result = await _service.GetSecretAsync("api-key");

        Assert.Equal("super-secret-123", result);
    }

    [Fact]
    public async Task SetSecretAsync_OverwritesExistingValue()
    {
        await _service.SetSecretAsync("api-key", "original");
        await _service.SetSecretAsync("api-key", "updated");

        var result = await _service.GetSecretAsync("api-key");

        Assert.Equal("updated", result);
    }
}
