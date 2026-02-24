using AzureAiFoundryCopilot.Infrastructure.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AzureAiFoundryCopilot.Api.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    private readonly KeyVaultOptions _keyVaultOptions;
    private readonly BlobStorageOptions _blobStorageOptions;
    private readonly EntraIdOptions _entraIdOptions;
    private readonly AppInsightsOptions _appInsightsOptions;

    public HealthController(
        IOptions<KeyVaultOptions> keyVaultOptions,
        IOptions<BlobStorageOptions> blobStorageOptions,
        IOptions<EntraIdOptions> entraIdOptions,
        IOptions<AppInsightsOptions> appInsightsOptions)
    {
        _keyVaultOptions = keyVaultOptions.Value;
        _blobStorageOptions = blobStorageOptions.Value;
        _entraIdOptions = entraIdOptions.Value;
        _appInsightsOptions = appInsightsOptions.Value;
    }

    [HttpGet]
    public IActionResult Get() =>
        Ok(new
        {
            status = "ok",
            service = "azure-ai-foundry-copilot-api",
            utcTime = DateTimeOffset.UtcNow,
            services = new
            {
                keyVaultEnabled = _keyVaultOptions.Enabled,
                blobStorageEnabled = _blobStorageOptions.Enabled,
                entraIdEnabled = _entraIdOptions.Enabled,
                appInsightsEnabled = _appInsightsOptions.Enabled
            }
        });
}
