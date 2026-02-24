using AzureAiFoundryCopilot.Application.Contracts;
using AzureAiFoundryCopilot.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AzureAiFoundryCopilot.Api.Controllers;

[ApiController]
[Route("api/ai-foundry")]
public sealed class AiFoundryController : ControllerBase
{
    private readonly IAiFoundryChatService _aiFoundryChatService;
    private readonly ILogger<AiFoundryController> _logger;

    public AiFoundryController(IAiFoundryChatService aiFoundryChatService, ILogger<AiFoundryController> logger)
    {
        _aiFoundryChatService = aiFoundryChatService;
        _logger = logger;
    }

    [HttpPost("chat")]
    [ProducesResponseType(typeof(AiChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Chat([FromBody] AiChatRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Bad request rejected: {@Errors}", ModelState);
            return ValidationProblem();
        }

        _logger.LogInformation("Chat request received — prompt length: {Length}", request.Prompt.Length);
        var response = await _aiFoundryChatService.CompleteAsync(request, cancellationToken);
        return Ok(response);
    }
}
