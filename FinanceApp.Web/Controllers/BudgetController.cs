using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Enums;
using FinanceApp.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FinanceApp.Infrastructure.Identity;

namespace FinanceApp.Web.Controllers;

[Authorize]
public class BudgetController : Controller
{
    private readonly IBudgetService _budgetService;
    private readonly ICategoryBudgetService _categoryBudgetService;
    private readonly ICategoryService _categoryService;
    private readonly UserManager<ApplicationUser> _userManager;

    public BudgetController(
        IBudgetService budgetService,
        ICategoryBudgetService categoryBudgetService,
        ICategoryService categoryService,
        UserManager<ApplicationUser> userManager)
    {
        _budgetService = budgetService;
        _categoryBudgetService = categoryBudgetService;
        _categoryService = categoryService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        var now = DateTime.Now;
        var budget = await _budgetService.GetBudgetForMonthAsync(userId, now.Month, now.Year);

        var model = new BudgetViewModel
        {
            Month = now.Month,
            Year = now.Year,
            Amount = budget?.Amount ?? 0,
            Currency = budget?.Currency ?? Currency.TZS
        };

        ViewBag.CurrentBudget = budget;
        ViewBag.MonthName = new DateTime(now.Year, now.Month, 1).ToString("MMMM yyyy");

        var categoryBudgets = await _categoryBudgetService.GetForMonthAsync(userId, now.Month, now.Year);
        var categoryBudgetItems = new List<CategoryBudgetItemViewModel>();
        var exceededCategories = new List<string>();
        foreach (var cb in categoryBudgets)
        {
            var spent = await _categoryBudgetService.GetCategorySpendAsync(userId, cb.CategoryId, now.Month, now.Year, cb.Currency);
            var isOver = spent >= cb.Amount;
            if (isOver)
                exceededCategories.Add($"{cb.Category?.Name ?? "Unknown"} ({spent:N0} / {cb.Amount:N0} {cb.Currency})");
            categoryBudgetItems.Add(new CategoryBudgetItemViewModel
            {
                Id = cb.Id,
                CategoryId = cb.CategoryId,
                CategoryName = cb.Category?.Name ?? "Unknown",
                Amount = cb.Amount,
                Currency = cb.Currency.ToString(),
                Spent = spent,
                IsOver = isOver,
                IsWarning = cb.Amount > 0 && spent >= cb.Amount * 0.8m && spent < cb.Amount
            });
        }
        ViewBag.CategoryBudgets = categoryBudgetItems;
        ViewBag.CategoryBudgetExceeded = exceededCategories;

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Set(BudgetViewModel model)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        if (model.Amount <= 0)
        {
            ModelState.AddModelError(nameof(BudgetViewModel.Amount), "Budget amount must be greater than 0.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.MonthName = new DateTime(model.Year, model.Month, 1).ToString("MMMM yyyy");
            return View("Index", model);
        }

        await _budgetService.SetBudgetAsync(userId, model.Month, model.Year, model.Amount, model.Currency);
        TempData["BudgetSaved"] = true;
        return RedirectToAction(nameof(Index));
    }

    // GET: /Budget/AddCategoryBudget?partial=true (for offcanvas)
    public async Task<IActionResult> AddCategoryBudget(bool partial = false)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        var now = DateTime.Now;
        var existing = await _categoryBudgetService.GetForMonthAsync(userId, now.Month, now.Year);
        var usedCategoryIds = existing.Select(cb => cb.CategoryId).ToHashSet();
        var expenseCategories = await _categoryService.GetCategoriesForExpenseAsync(userId);
        var availableCategories = expenseCategories.Where(c => !usedCategoryIds.Contains(c.Id)).ToList();

        var model = new CategoryBudgetViewModel
        {
            Month = now.Month,
            Year = now.Year,
            Currency = Currency.TZS
        };

        ViewBag.AvailableCategories = availableCategories;
        ViewBag.MonthName = new DateTime(now.Year, now.Month, 1).ToString("MMMM yyyy");

        bool isAjax = partial || string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        if (isAjax)
            return PartialView("_CategoryBudgetFormPartial", model);

        return View("Index", new BudgetViewModel { Month = now.Month, Year = now.Year });
    }

    // POST: /Budget/SetCategoryBudget (supports both full form POST and AJAX)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCategoryBudget(CategoryBudgetViewModel model)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        if (model.Amount <= 0)
            ModelState.AddModelError(nameof(CategoryBudgetViewModel.Amount), "Budget amount must be greater than 0.");

        if (!ModelState.IsValid)
        {
            ViewBag.AvailableCategories = (await _categoryService.GetCategoriesForExpenseAsync(userId)).ToList();
            ViewBag.MonthName = new DateTime(model.Year, model.Month, 1).ToString("MMMM yyyy");
            var isAjax = string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
            if (isAjax)
                return PartialView("_CategoryBudgetFormPartial", model);
            TempData["CategoryBudgetError"] = "Please fix the validation errors.";
            return RedirectToAction(nameof(Index));
        }

        await _categoryBudgetService.SetAsync(userId, model.CategoryId, model.Month, model.Year, model.Amount, model.Currency);
        TempData["CategoryBudgetSaved"] = true;
        var isAjaxPost = string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        if (isAjaxPost)
            return Json(new { redirect = Url.Action(nameof(Index)) });
        return RedirectToAction(nameof(Index));
    }

    // POST: /Budget/DeleteCategoryBudget
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategoryBudget(Guid id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        await _categoryBudgetService.DeleteAsync(id, userId);
        TempData["CategoryBudgetDeleted"] = true;
        return RedirectToAction(nameof(Index));
    }
}
