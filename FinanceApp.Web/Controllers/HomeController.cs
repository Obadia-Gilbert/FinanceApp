using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using FinanceApp.Web.Models;
using FinanceApp.Infrastructure.Identity;
using FinanceApp.Application.Interfaces.Services;
using System.Text.Json;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly IExpenseService _expenseService;
    private readonly ICategoryService _categoryService;
    private readonly IBudgetService _budgetService;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(
        IExpenseService expenseService,
        ICategoryService categoryService,
        IBudgetService budgetService,
        UserManager<ApplicationUser> userManager)
    {
        _expenseService = expenseService;
        _categoryService = categoryService;
        _budgetService = budgetService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        // Fetch all user expenses
        var allExpenses = await _expenseService.GetPagedExpensesAsync(
            pageNumber: 1,
            pageSize: 1000,
            filter: e => e.UserId == userId,
            orderBy: q => q.OrderByDescending(e => e.ExpenseDate)
        );

        var expenses = allExpenses.Items.ToList();

        // Get user's categories (all categories assigned to this user, not just ones used)
        var pagedCategories = await _categoryService.GetPagedCategoriesAsync(pageNumber: 1, pageSize: 100, userId: userId);
        var categoryCount = pagedCategories.TotalItems;

        // Display currency: most-used currency in user's expenses; default TZS if none
        var displayCurrency = expenses
            .GroupBy(e => e.Currency)
            .OrderByDescending(g => g.Sum(x => x.Amount))
            .Select(g => g.Key)
            .FirstOrDefault();
        if (expenses.Count == 0)
            displayCurrency = Currency.TZS; // default label when no data

        // Total spend: sum only amounts in the display currency (no mixing currencies)
        var totalSpend = expenses.Where(e => e.Currency == displayCurrency).Sum(e => e.Amount);
        var expenseCount = expenses.Count;
        var currentMonth = DateTime.Now.Month;
        var currentYear = DateTime.Now.Year;
        var thisMonthExpense = expenses
            .Where(e => e.Currency == displayCurrency && e.ExpenseDate.Month == currentMonth && e.ExpenseDate.Year == currentYear)
            .Sum(e => e.Amount);

        // Chart: last 30 days, only expenses in display currency, grouped by date
        var last30Days = DateTime.Now.AddDays(-30);
        var last30Expenses = expenses
            .Where(e => e.Currency == displayCurrency && e.ExpenseDate >= last30Days)
            .GroupBy(e => e.ExpenseDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new { Date = g.Key.ToString("MMM dd"), Amount = g.Sum(e => e.Amount) })
            .ToList();

        // Budget: current month budget and spend in same currency for alert
        var currentMonthBudget = await _budgetService.GetBudgetForMonthAsync(userId, currentMonth, currentYear);
        decimal? budgetAmount = currentMonthBudget?.Amount;
        var budgetCurrency = currentMonthBudget?.Currency ?? displayCurrency;
        var thisMonthSpendInBudgetCurrency = expenses
            .Where(e => e.Currency == budgetCurrency && e.ExpenseDate.Month == currentMonth && e.ExpenseDate.Year == currentYear)
            .Sum(e => e.Amount);
        var isOverBudget = budgetAmount.HasValue && budgetAmount.Value > 0 && thisMonthSpendInBudgetCurrency >= budgetAmount.Value;

        // Pass data to view: amounts in display currency, with currency code for label
        ViewBag.TotalSpend = totalSpend;
        ViewBag.DisplayCurrency = displayCurrency.ToString();
        ViewBag.ExpenseCount = expenseCount;
        ViewBag.CategoryCount = categoryCount;
        ViewBag.ThisMonthSpend = thisMonthExpense;
        ViewBag.ChartLabels = JsonSerializer.Serialize(last30Expenses.Select(x => x.Date).ToList());
        ViewBag.ChartData = JsonSerializer.Serialize(last30Expenses.Select(x => x.Amount).ToList());
        ViewBag.ChartCurrency = displayCurrency.ToString();
        ViewBag.BudgetAmount = budgetAmount;
        ViewBag.BudgetCurrency = budgetCurrency.ToString();
        ViewBag.ThisMonthSpendInBudgetCurrency = thisMonthSpendInBudgetCurrency;
        ViewBag.IsOverBudget = isOverBudget;

        return View();
    }

    [AllowAnonymous]
    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
