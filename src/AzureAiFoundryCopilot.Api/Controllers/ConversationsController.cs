using AzureAiFoundryCopilot.Application.Contracts;
using AzureAiFoundryCopilot.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AzureAiFoundryCopilot.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/conversations")]
public sealed class ConversationsController : ControllerBase
{
    private readonly IConversationStorageService _storageService;

    public ConversationsController(IConversationStorageService storageService)
    {
        _storageService = storageService;
    }

    [HttpGet("{conversationId}")]
    [ProducesResponseType(typeof(ChatConversation), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string conversationId, CancellationToken cancellationToken)
    {
        var conversation = await _storageService.GetAsync(conversationId, cancellationToken);
        return conversation is not null ? Ok(conversation) : NotFound();
    }

    [HttpGet("recent")]
    [ProducesResponseType(typeof(IReadOnlyList<ChatConversation>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecent([FromQuery] int count = 10, CancellationToken cancellationToken = default)
    {
        var conversations = await _storageService.ListRecentAsync(count, cancellationToken);
        return Ok(conversations);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ChatConversation), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Save([FromBody] ChatConversation conversation, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(conversation.ConversationId))
            return BadRequest("ConversationId is required.");

        await _storageService.SaveAsync(conversation, cancellationToken);
        return CreatedAtAction(nameof(Get), new { conversationId = conversation.ConversationId }, conversation);
    }
}
