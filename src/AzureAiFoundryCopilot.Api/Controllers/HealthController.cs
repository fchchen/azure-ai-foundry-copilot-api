using Microsoft.AspNetCore.Mvc;

namespace AzureAiFoundryCopilot.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() =>
        Ok(new
        {
            status = "ok",
            service = "azure-ai-foundry-copilot-api",
            utcTime = DateTimeOffset.UtcNow
        });
}
