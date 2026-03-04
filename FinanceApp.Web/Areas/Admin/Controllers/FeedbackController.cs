using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Identity;

namespace FinanceApp.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class FeedbackController : Controller
{
    private readonly IFeedbackService _feedbackService;
    private readonly UserManager<ApplicationUser> _userManager;

    public FeedbackController(IFeedbackService feedbackService, UserManager<ApplicationUser> userManager)
    {
        _feedbackService = feedbackService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(FeedbackStatus? status = null, FeedbackType? type = null, int pageNumber = 1, int pageSize = 20)
    {
        var paged = await _feedbackService.GetPagedForAdminAsync(pageNumber, pageSize, status, type);
        var userIds = paged.Items.Select(f => f.UserId).Distinct().ToList();
        var userEmails = new Dictionary<string, string>();
        foreach (var id in userIds)
        {
            var user = await _userManager.FindByIdAsync(id);
            userEmails[id] = user?.Email ?? id;
        }

        ViewBag.UserEmails = userEmails;
        ViewBag.PageNumber = paged.PageNumber;
        ViewBag.PageSize = paged.PageSize;
        ViewBag.TotalItems = paged.TotalItems;
        ViewBag.TotalPages = paged.TotalPages;
        ViewBag.StatusFilter = status;
        ViewBag.TypeFilter = type;
        ViewBag.FeedbackStatuses = Enum.GetValues<FeedbackStatus>();
        ViewBag.FeedbackTypes = Enum.GetValues<FeedbackType>();
        return View(paged.Items);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(Guid id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var feedback = await _feedbackService.GetByIdAsync(id, userId, isAdmin: true);
        if (feedback == null) return NotFound();

        var user = await _userManager.FindByIdAsync(feedback.UserId);
        ViewBag.UserEmail = user?.Email ?? feedback.UserId;
        ViewBag.FeedbackStatuses = Enum.GetValues<FeedbackStatus>();
        return View(feedback);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        await _feedbackService.MarkAsReadAsync(id);
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(Guid id, FeedbackStatus status)
    {
        await _feedbackService.SetStatusAsync(id, status);
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAdminNotes(Guid id, string? adminNotes)
    {
        await _feedbackService.SetAdminNotesAsync(id, adminNotes);
        return RedirectToAction(nameof(Detail), new { id });
    }
}
