using AzureAiFoundryCopilot.Application.Contracts;
using AzureAiFoundryCopilot.Application.Exceptions;
using AzureAiFoundryCopilot.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AzureAiFoundryCopilot.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/ai-foundry")]
public sealed class AiFoundryController : ControllerBase
{
    private readonly IAiFoundryOrchestrationService _orchestrationService;

    public AiFoundryController(
        IAiFoundryOrchestrationService orchestrationService)
    {
        _orchestrationService = orchestrationService;
    }

    [HttpPost("chat")]
    [ProducesResponseType(typeof(AiChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Chat([FromBody] AiChatRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem();

        var response = await _orchestrationService.CompleteAndPersistConversationAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("unread-email-summary")]
    [ProducesResponseType(typeof(UnreadEmailSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SummarizeUnreadEmails(
        [FromBody] UnreadEmailSummaryRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem();

        try
        {
            var accessToken = await HttpContext.GetTokenAsync("access_token");
            var response = await _orchestrationService.SummarizeUnreadEmailsAsync(request, accessToken, cancellationToken);
            return Ok(response);
        }
        catch (AccessTokenRequiredException ex)
        {
            return Unauthorized(ex.Message);
        }
    }
}
