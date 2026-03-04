using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Identity;
using FinanceApp.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.Web.Controllers;

[Authorize]
public class RecurringController : Controller
{
    private readonly IRecurringTemplateService _recurringService;
    private readonly IAccountService _accountService;
    private readonly ICategoryService _categoryService;
    private readonly UserManager<ApplicationUser> _userManager;

    public RecurringController(
        IRecurringTemplateService recurringService,
        IAccountService accountService,
        ICategoryService categoryService,
        UserManager<ApplicationUser> userManager)
    {
        _recurringService = recurringService;
        _accountService = accountService;
        _categoryService = categoryService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 20)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        var paged = await _recurringService.GetPagedAsync(userId, pageNumber, pageSize);
        var viewModels = paged.Items.Select(r => new RecurringTemplateListViewModel
        {
            Id = r.Id,
            AccountName = r.Account?.Name ?? "",
            CategoryName = r.Category?.Name,
            Type = r.Type,
            Amount = r.Amount,
            Currency = r.Currency,
            Frequency = r.Frequency,
            Interval = r.Interval,
            NextRunDate = r.NextRunDate.DateTime,
            EndDate = r.EndDate?.DateTime,
            Note = r.Note
        }).ToList();

        ViewBag.PageNumber = pageNumber;
        ViewBag.TotalPages = (int)Math.Ceiling(paged.TotalItems / (double)pageSize);
        ViewBag.TotalItems = paged.TotalItems;
        return View(viewModels);
    }

    public async Task<IActionResult> Create()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        ViewBag.Accounts = await _accountService.GetAllAsync(userId);
        ViewBag.Categories = await _categoryService.GetAllAsync(userId);
        ViewBag.Currencies = Enum.GetValues<Currency>();
        ViewBag.Frequencies = Enum.GetValues<RecurrenceFrequency>();
        return View(new RecurringTemplateCreateViewModel { StartDate = DateTime.Today });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RecurringTemplateCreateViewModel model)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        if (!ModelState.IsValid)
        {
            ViewBag.Accounts = await _accountService.GetAllAsync(userId);
            ViewBag.Categories = await _categoryService.GetAllAsync(userId);
            ViewBag.Currencies = Enum.GetValues<Currency>();
            ViewBag.Frequencies = Enum.GetValues<RecurrenceFrequency>();
            return View(model);
        }

        await _recurringService.CreateAsync(
            userId,
            model.AccountId,
            model.Type,
            model.Amount,
            model.Currency,
            model.Frequency,
            new DateTimeOffset(model.StartDate),
            model.CategoryId == Guid.Empty ? null : model.CategoryId,
            model.Note,
            model.Interval,
            model.EndDate.HasValue ? new DateTimeOffset(model.EndDate.Value) : null);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        await _recurringService.DeactivateAsync(id, userId);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        await _recurringService.DeleteAsync(id, userId);
        return RedirectToAction(nameof(Index));
    }
}
