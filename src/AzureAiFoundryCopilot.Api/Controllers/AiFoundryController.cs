using AzureAiFoundryCopilot.Application.Contracts;
using AzureAiFoundryCopilot.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AzureAiFoundryCopilot.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/ai-foundry")]
public sealed class AiFoundryController : ControllerBase
{
    private readonly IAiFoundryChatService _aiFoundryChatService;
    private readonly IConversationStorageService _conversationStorage;
    private readonly ILogger<AiFoundryController> _logger;

    public AiFoundryController(
        IAiFoundryChatService aiFoundryChatService,
        IConversationStorageService conversationStorage,
        ILogger<AiFoundryController> logger)
    {
        _aiFoundryChatService = aiFoundryChatService;
        _conversationStorage = conversationStorage;
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

        var conversation = new ChatConversation(
            ConversationId: Guid.NewGuid().ToString("N"),
            UserPrompt: request.Prompt,
            AiResponse: response.Completion,
            CreatedAtUtc: response.CreatedAtUtc);

        await _conversationStorage.SaveAsync(conversation, cancellationToken);

        return Ok(response);
    }
}
