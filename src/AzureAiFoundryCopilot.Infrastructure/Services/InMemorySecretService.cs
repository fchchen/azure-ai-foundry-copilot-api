using System.Collections.Concurrent;
using AzureAiFoundryCopilot.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AzureAiFoundryCopilot.Infrastructure.Services;

public sealed class InMemorySecretService : ISecretService
{
    private readonly ConcurrentDictionary<string, string> _secrets = new();
    private readonly ILogger<InMemorySecretService> _logger;

    public InMemorySecretService(ILogger<InMemorySecretService> logger)
    {
        _logger = logger;
    }

    public Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving secret {SecretName} from in-memory store.", name);

        _secrets.TryGetValue(name, out var value);
        return Task.FromResult(value);
    }

    public Task SetSecretAsync(string name, string value, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Setting secret {SecretName} in in-memory store.", name);

        _secrets[name] = value;
        return Task.CompletedTask;
    }
}
