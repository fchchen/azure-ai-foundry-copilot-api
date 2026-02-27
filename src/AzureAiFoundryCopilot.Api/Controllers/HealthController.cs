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
    private readonly IWebHostEnvironment _environment;
    private readonly KeyVaultOptions _keyVaultOptions;
    private readonly BlobStorageOptions _blobStorageOptions;
    private readonly MicrosoftGraphOptions _graphOptions;
    private readonly EntraIdOptions _entraIdOptions;
    private readonly AppInsightsOptions _appInsightsOptions;

    public HealthController(
        IWebHostEnvironment environment,
        IOptions<KeyVaultOptions> keyVaultOptions,
        IOptions<BlobStorageOptions> blobStorageOptions,
        IOptions<MicrosoftGraphOptions> graphOptions,
        IOptions<EntraIdOptions> entraIdOptions,
        IOptions<AppInsightsOptions> appInsightsOptions)
    {
        _environment = environment;
        _keyVaultOptions = keyVaultOptions.Value;
        _blobStorageOptions = blobStorageOptions.Value;
        _graphOptions = graphOptions.Value;
        _entraIdOptions = entraIdOptions.Value;
        _appInsightsOptions = appInsightsOptions.Value;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var basic = new
        {
            status = "ok",
            service = "azure-ai-foundry-copilot-api",
            utcTime = DateTimeOffset.UtcNow
        };

        if (!_environment.IsDevelopment())
            return Ok(basic);

        return Ok(new
        {
            basic.status,
            basic.service,
            basic.utcTime,
            services = new
            {
                keyVaultEnabled = _keyVaultOptions.Enabled,
                blobStorageEnabled = _blobStorageOptions.Enabled,
                microsoftGraphEnabled = _graphOptions.Enabled,
                entraIdEnabled = _entraIdOptions.Enabled,
                appInsightsEnabled = _appInsightsOptions.Enabled
            }
        });
    }
}
