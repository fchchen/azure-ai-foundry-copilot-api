using AzureAiFoundryCopilot.Application.Contracts;
using AzureAiFoundryCopilot.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AzureAiFoundryCopilot.Api.Controllers;

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
}
