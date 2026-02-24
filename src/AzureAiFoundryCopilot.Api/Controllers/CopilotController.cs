using AzureAiFoundryCopilot.Application.Contracts;
using AzureAiFoundryCopilot.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AzureAiFoundryCopilot.Api.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/copilot")]
public sealed class CopilotController : ControllerBase
{
    private readonly ICopilotManifestService _copilotManifestService;

    public CopilotController(ICopilotManifestService copilotManifestService)
    {
        _copilotManifestService = copilotManifestService;
    }

    [HttpGet("manifest")]
    [ProducesResponseType(typeof(CopilotManifestResponse), StatusCodes.Status200OK)]
    public IActionResult GetManifest() => Ok(_copilotManifestService.GetManifest());

    [HttpGet("openapi")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetOpenApiSpec()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appPackage", "openapi.json");
        if (!System.IO.File.Exists(path))
            return NotFound();

        return PhysicalFile(path, "application/json");
    }

    [HttpGet("ai-plugin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetAiPlugin()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appPackage", "ai-plugin.json");
        if (!System.IO.File.Exists(path))
            return NotFound();

        return PhysicalFile(path, "application/json");
    }
}
