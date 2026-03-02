using System.Security.Claims;
using FinanceApp.API.DTOs;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IExpenseService _expenseService;
    private readonly ICategoryService _categoryService;
    private readonly IBudgetService _budgetService;
    private readonly ICategoryBudgetService _categoryBudgetService;

    public DashboardController(
        IExpenseService expenseService,
        ICategoryService categoryService,
        IBudgetService budgetService,
        ICategoryBudgetService categoryBudgetService)
    {
        _expenseService = expenseService;
        _categoryService = categoryService;
        _budgetService = budgetService;
        _categoryBudgetService = categoryBudgetService;
    }

    private string? UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [HttpGet]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDashboard()
    {
        if (UserId == null) return Unauthorized();

        var allExpenses = await _expenseService.GetPagedExpensesAsync(
            pageNumber: 1,
            pageSize: 1000,
            filter: e => e.UserId == UserId,
            orderBy: q => q.OrderByDescending(e => e.ExpenseDate));

        var expenses = allExpenses.Items.ToList();

        var pagedCategories = await _categoryService.GetPagedCategoriesAsync(
            pageNumber: 1,
            pageSize: 100,
            userId: UserId);

        var categoryCount = pagedCategories.TotalItems;

        var displayCurrency = expenses
            .GroupBy(e => e.Currency)
            .OrderByDescending(g => g.Sum(x => x.Amount))
            .Select(g => g.Key)
            .FirstOrDefault();
        if (expenses.Count == 0)
            displayCurrency = Currency.TZS;

        var totalSpend = expenses
            .Where(e => e.Currency == displayCurrency)
            .Sum(e => e.Amount);

        var expenseCount = expenses.Count;
        var currentMonth = DateTime.Now.Month;
        var currentYear = DateTime.Now.Year;

        var thisMonthExpense = expenses
            .Where(e => e.Currency == displayCurrency &&
                        e.ExpenseDate.Month == currentMonth &&
                        e.ExpenseDate.Year == currentYear)
            .Sum(e => e.Amount);

        var last30Days = DateTime.Now.AddDays(-30);
        var chartData = expenses
            .Where(e => e.Currency == displayCurrency && e.ExpenseDate >= last30Days)
            .GroupBy(e => e.ExpenseDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new ChartDataPoint(g.Key.ToString("MMM dd"), g.Sum(e => e.Amount)))
            .ToList();

        var currentMonthBudget = await _budgetService.GetBudgetForMonthAsync(
            UserId, currentMonth, currentYear);
        decimal? budgetAmount = currentMonthBudget?.Amount;
        var budgetCurrency = currentMonthBudget?.Currency ?? displayCurrency;
        var thisMonthSpendInBudgetCurrency = expenses
            .Where(e => e.Currency == budgetCurrency &&
                        e.ExpenseDate.Month == currentMonth &&
                        e.ExpenseDate.Year == currentYear)
            .Sum(e => e.Amount);
        var isOverBudget = budgetAmount.HasValue &&
                           budgetAmount.Value > 0 &&
                           thisMonthSpendInBudgetCurrency >= budgetAmount.Value;

        var categoryBudgets = await _categoryBudgetService.GetForMonthAsync(
            UserId, currentMonth, currentYear);
        var categoryBudgetAlerts = new List<CategoryBudgetAlertDto>();
        foreach (var cb in categoryBudgets)
        {
            var spent = await _categoryBudgetService.GetCategorySpendAsync(
                UserId, cb.CategoryId, currentMonth, currentYear, cb.Currency);
            if (spent >= cb.Amount)
                categoryBudgetAlerts.Add(new CategoryBudgetAlertDto(
                    cb.Category?.Name ?? "Unknown", spent, cb.Amount, cb.Currency.ToString(), true));
            else if (cb.Amount > 0 && spent >= cb.Amount * 0.8m)
                categoryBudgetAlerts.Add(new CategoryBudgetAlertDto(
                    cb.Category?.Name ?? "Unknown", spent, cb.Amount, cb.Currency.ToString(), false));
        }

        var dto = new DashboardDto(
            totalSpend,
            displayCurrency.ToString(),
            expenseCount,
            categoryCount,
            thisMonthExpense,
            budgetAmount,
            budgetCurrency.ToString(),
            isOverBudget,
            chartData,
            categoryBudgetAlerts);

        return Ok(dto);
    }
}
