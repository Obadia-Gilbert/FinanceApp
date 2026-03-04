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
    private readonly IExpenseQueryService _expenseQueryService;
    private readonly ICategoryService _categoryService;
    private readonly IBudgetService _budgetService;
    private readonly ICategoryBudgetService _categoryBudgetService;
    private readonly INotificationService _notificationService;

    public DashboardController(
        IExpenseQueryService expenseQueryService,
        ICategoryService categoryService,
        IBudgetService budgetService,
        ICategoryBudgetService categoryBudgetService,
        INotificationService notificationService)
    {
        _expenseQueryService = expenseQueryService;
        _categoryService = categoryService;
        _budgetService = budgetService;
        _categoryBudgetService = categoryBudgetService;
        _notificationService = notificationService;
    }

    private string? UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [HttpGet]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDashboard()
    {
        if (UserId == null) return Unauthorized();

        var currentMonth = DateTime.Now.Month;
        var currentYear = DateTime.Now.Year;
        var last30Start = DateTimeOffset.UtcNow.AddDays(-30);
        var last30End = DateTimeOffset.UtcNow.AddDays(1);

        // Sequential: DbContext is not thread-safe for concurrent operations
        var totalsByCurrency = await _expenseQueryService.GetTotalsByCurrencyAsync(UserId);
        var thisMonthTotals = await _expenseQueryService.GetMonthTotalsByCurrencyAsync(UserId, currentMonth, currentYear);
        var expenseCount = await _expenseQueryService.GetTotalCountAsync(UserId);
        var pagedCategories = await _categoryService.GetPagedCategoriesAsync(pageNumber: 1, pageSize: 100, userId: UserId);
        var categoryCount = pagedCategories.TotalItems;

        var displayCurrency = totalsByCurrency.Count > 0
            ? totalsByCurrency.OrderByDescending(kv => kv.Value).Select(kv => kv.Key).First()
            : Currency.TZS;
        var totalSpend = totalsByCurrency.GetValueOrDefault(displayCurrency, 0);
        var thisMonthExpense = thisMonthTotals.GetValueOrDefault(displayCurrency, 0);

        var trendSums = await _expenseQueryService.GetSumsByDayAsync(UserId, last30Start, last30End, displayCurrency);
        var trendByDate = trendSums.ToDictionary(s => s.Date, s => s.Sum);
        var now = DateTime.Now;
        var chartData = Enumerable.Range(0, 30)
            .Select(i => now.Date.AddDays(-29 + i))
            .Select(d => new ChartDataPoint(d.ToString("MMM dd"), trendByDate.GetValueOrDefault(d, 0m)))
            .ToList();

        var currentMonthBudget = await _budgetService.GetBudgetForMonthAsync(UserId, currentMonth, currentYear);
        decimal? budgetAmount = currentMonthBudget?.Amount;
        var budgetCurrency = currentMonthBudget?.Currency ?? displayCurrency;
        var thisMonthSpendInBudgetCurrency = thisMonthTotals.GetValueOrDefault(budgetCurrency, 0);
        var isOverBudget = budgetAmount.HasValue && budgetAmount.Value > 0 && thisMonthSpendInBudgetCurrency >= budgetAmount.Value;

        var categorySpendForMonth = await _categoryBudgetService.GetCategorySpendForMonthAsync(UserId, currentMonth, currentYear);
        var categoryBudgets = await _categoryBudgetService.GetForMonthAsync(UserId, currentMonth, currentYear);
        var categoryBudgetAlerts = new List<CategoryBudgetAlertDto>();
        foreach (var cb in categoryBudgets)
        {
            var spent = categorySpendForMonth.GetValueOrDefault((cb.CategoryId, cb.Currency), 0);
            var catName = cb.Category?.Name ?? "Unknown";
            if (spent >= cb.Amount)
            {
                categoryBudgetAlerts.Add(new CategoryBudgetAlertDto(catName, spent, cb.Amount, cb.Currency.ToString(), true));
                var topicKey = $"budget-category-{cb.CategoryId}-{currentYear}-{currentMonth}";
                await _notificationService.CreateIfNotExistsAsync(UserId!,
                    "Category budget exceeded",
                    $"{catName}: {spent:N0} {cb.Currency} of {cb.Amount:N0} {cb.Currency} ({(spent / cb.Amount * 100):F0}%).",
                    NotificationType.CategoryBudgetExceeded, "/Budget", topicKey);
            }
            else if (cb.Amount > 0 && spent >= cb.Amount * 0.8m)
            {
                categoryBudgetAlerts.Add(new CategoryBudgetAlertDto(catName, spent, cb.Amount, cb.Currency.ToString(), false));
                var topicKey = $"budget-category-{cb.CategoryId}-{currentYear}-{currentMonth}";
                await _notificationService.CreateIfNotExistsAsync(UserId!,
                    "Category budget warning",
                    $"{catName}: {spent:N0} {cb.Currency} of {cb.Amount:N0} {cb.Currency} ({(spent / cb.Amount * 100):F0}%).",
                    NotificationType.CategoryBudgetWarning, "/Budget", topicKey);
            }
        }

        if (isOverBudget && budgetAmount.HasValue)
        {
            var topicKey = $"budget-global-{currentYear}-{currentMonth}";
            await _notificationService.CreateIfNotExistsAsync(UserId!,
                "Monthly budget exceeded",
                $"You've spent {thisMonthSpendInBudgetCurrency:N0} {budgetCurrency} against a budget of {budgetAmount.Value:N0} {budgetCurrency}.",
                NotificationType.BudgetExceeded, "/Budget", topicKey);
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
