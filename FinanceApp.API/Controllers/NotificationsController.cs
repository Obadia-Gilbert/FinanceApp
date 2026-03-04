using System.Security.Claims;
using FinanceApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    private string? UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(UnreadCountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var count = await _notificationService.GetUnreadCountAsync(userId);
        return Ok(new UnreadCountResponse(count));
    }

    [HttpGet]
    [ProducesResponseType(typeof(NotificationListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var result = await _notificationService.GetByUserAsync(userId, page, pageSize);
        var items = result.Items.Select(n => new NotificationItemDto(
            n.Id,
            n.Title,
            n.Message,
            n.Type.ToString(),
            n.RelatedLink,
            n.IsRead,
            n.CreatedAt
        )).ToList();
        return Ok(new NotificationListResponse(items, result.TotalItems, result.PageNumber, result.PageSize));
    }

    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var userId = UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var ok = await _notificationService.MarkAsReadAsync(id, userId);
        return ok ? Ok(new { success = true }) : NotFound();
    }

    [HttpPost("mark-all-read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        await _notificationService.MarkAllAsReadAsync(userId);
        return Ok(new { success = true });
    }

    public record UnreadCountResponse(int Count);
    public record NotificationItemDto(Guid Id, string Title, string Message, string Type, string? RelatedLink, bool IsRead, DateTimeOffset CreatedAt);
    public record NotificationListResponse(IReadOnlyList<NotificationItemDto> Items, int TotalItems, int PageNumber, int PageSize);
}
