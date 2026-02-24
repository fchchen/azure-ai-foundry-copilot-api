using Azure.Security.KeyVault.Secrets;
using AzureAiFoundryCopilot.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AzureAiFoundryCopilot.Infrastructure.Services;

public sealed class KeyVaultSecretService : ISecretService
{
    private readonly SecretClient _secretClient;
    private readonly ILogger<KeyVaultSecretService> _logger;

    public KeyVaultSecretService(SecretClient secretClient, ILogger<KeyVaultSecretService> logger)
    {
        _secretClient = secretClient;
        _logger = logger;
    }

    public async Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving secret {SecretName} from Key Vault.", name);

        var response = await _secretClient.GetSecretAsync(name, cancellationToken: cancellationToken);
        return response.Value.Value;
    }

    public async Task SetSecretAsync(string name, string value, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Setting secret {SecretName} in Key Vault.", name);

        await _secretClient.SetSecretAsync(name, value, cancellationToken);
    }
}
