using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Infrastructure.Identity;

namespace FinanceApp.Web.Controllers;

[Authorize]
public class NotificationController : Controller
{
    private readonly INotificationService _notificationService;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationController(INotificationService notificationService, UserManager<ApplicationUser> userManager)
    {
        _notificationService = notificationService;
        _userManager = userManager;
    }

    private string? _userId => _userManager.GetUserId(User);

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 25)
    {
        var userId = _userId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var result = await _notificationService.GetByUserAsync(userId, page, pageSize);
        ViewBag.Notifications = result.Items;
        ViewBag.TotalItems = result.TotalItems;
        ViewBag.PageNumber = result.PageNumber;
        ViewBag.PageSize = result.PageSize;
        ViewBag.TotalPages = result.TotalPages;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> UnreadCount()
    {
        var userId = _userId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var count = await _notificationService.GetUnreadCountAsync(userId);
        return Json(new { count });
    }

    [HttpGet]
    public async Task<IActionResult> List(int page = 1, int pageSize = 20)
    {
        var userId = _userId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var result = await _notificationService.GetByUserAsync(userId, page, pageSize);
        var items = result.Items.Select(n => new
        {
            n.Id,
            n.Title,
            n.Message,
            n.Type,
            n.RelatedLink,
            n.IsRead,
            CreatedAt = n.CreatedAt.ToString("O")
        }).ToList();
        return Json(new { items, result.TotalItems, result.PageNumber, result.PageSize });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> MarkRead([FromBody] MarkReadRequest? request)
    {
        var userId = _userId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        if (request?.Id == null) return BadRequest();
        var ok = await _notificationService.MarkAsReadAsync(request.Id.Value, userId);
        return ok ? Json(new { success = true }) : NotFound();
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = _userId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        await _notificationService.MarkAllAsReadAsync(userId);
        return Json(new { success = true });
    }

    public class MarkReadRequest
    {
        public Guid? Id { get; set; }
    }
}
