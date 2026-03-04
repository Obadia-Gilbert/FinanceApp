using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Identity;
using FinanceApp.Web.Models;

namespace FinanceApp.Web.Controllers;

[Authorize]
public class FeedbackController : Controller
{
    private readonly IFeedbackService _feedbackService;
    private readonly UserManager<ApplicationUser> _userManager;

    public FeedbackController(IFeedbackService feedbackService, UserManager<ApplicationUser> userManager)
    {
        _feedbackService = feedbackService;
        _userManager = userManager;
    }

    private string? UserId => _userManager.GetUserId(User);

    public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 15)
    {
        var userId = UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var paged = await _feedbackService.GetMyAsync(userId, pageNumber, pageSize);
        var model = paged.Items.Select(f => new FeedbackListViewModel
        {
            Id = f.Id,
            Type = f.Type,
            Subject = f.Subject,
            Message = f.Message,
            CreatedAt = f.CreatedAt
        }).ToList();

        ViewBag.PageNumber = pageNumber;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalItems = paged.TotalItems;
        ViewBag.TotalPages = (int)Math.Ceiling(paged.TotalItems / (double)pageSize);
        ViewBag.FeedbackTypes = Enum.GetValues<FeedbackType>();
        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.FeedbackTypes = Enum.GetValues<FeedbackType>();
        return View(new FeedbackCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FeedbackCreateViewModel model)
    {
        var userId = UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        if (ModelState.IsValid)
        {
            await _feedbackService.CreateAsync(userId, model.Type, model.Message.Trim(), model.Subject?.Trim());
            TempData["FeedbackSuccess"] = "Thank you! Your feedback has been submitted. Our team will review it.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.FeedbackTypes = Enum.GetValues<FeedbackType>();
        return View(model);
    }
}
