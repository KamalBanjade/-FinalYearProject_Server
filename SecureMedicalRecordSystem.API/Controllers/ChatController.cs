using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureMedicalRecordSystem.Core.DTOs;
using SecureMedicalRecordSystem.Core.Interfaces;
using System.Security.Claims;

namespace SecureMedicalRecordSystem.API.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    /// <summary>Get paginated message history with another user.</summary>
    [HttpGet("conversation/{otherUserId:guid}")]
    public async Task<IActionResult> GetConversation(
        [FromRoute] Guid otherUserId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var userId = GetCurrentUserId();

        var canMessage = await _chatService.CanUsersMessageAsync(userId.ToString(), otherUserId.ToString());
        if (!canMessage)
            return StatusCode(403, ApiResponse.FailureResult("You are not authorized to view this conversation."));

        var messages = await _chatService.GetConversationAsync(userId, otherUserId, page, pageSize);
        return Ok(ApiResponse.SuccessResult(messages, "Conversation retrieved."));
    }

    /// <summary>Get all conversations for the current user.</summary>
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        var userId = GetCurrentUserId();
        var conversations = await _chatService.GetUserConversationsAsync(userId);
        return Ok(ApiResponse.SuccessResult(conversations, "Conversations retrieved."));
    }

    /// <summary>Get total unread message count for the current user.</summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetCurrentUserId();
        var count = await _chatService.GetUnreadCountAsync(userId);
        return Ok(ApiResponse.SuccessResult(new { unreadCount = count }, "Unread count retrieved."));
    }

    /// <summary>Check whether a given user is currently online.</summary>
    [HttpGet("online-status/{userId:guid}")]
    public async Task<IActionResult> CheckOnlineStatus([FromRoute] Guid userId)
    {
        var isOnline = await _chatService.IsUserOnlineAsync(userId.ToString());
        var lastSeenAt = await _chatService.GetLastSeenAsync(userId.ToString());
        return Ok(ApiResponse.SuccessResult(new { isOnline, lastSeenAt }, "Online status retrieved."));
    }

    /// <summary>Permanently hard-deletes the conversation history between the current user and the target user.</summary>
    [HttpDelete("conversation/{otherUserId:guid}")]
    public async Task<IActionResult> DeleteConversation([FromRoute] Guid otherUserId)
    {
        var userId = GetCurrentUserId();
        await _chatService.DeleteConversationAsync(userId, otherUserId);
        return Ok(ApiResponse.SuccessResult((object?)null, "Conversation permanently deleted."));
    }

    // ── Private Helpers ───────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claim) || !Guid.TryParse(claim, out var userId))
            throw new UnauthorizedAccessException("Could not determine current user.");
        return userId;
    }
}
