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
    private readonly UserManager<ApplicationUser> _userManager;

    public BudgetController(IBudgetService budgetService, UserManager<ApplicationUser> userManager)
    {
        _budgetService = budgetService;
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
}
