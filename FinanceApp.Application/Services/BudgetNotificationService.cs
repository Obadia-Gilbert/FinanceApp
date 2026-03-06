using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Services;

public class BudgetNotificationService : IBudgetNotificationService
{
    private readonly IBudgetService _budgetService;
    private readonly ICategoryBudgetService _categoryBudgetService;
    private readonly IExpenseQueryService _expenseQueryService;
    private readonly INotificationService _notificationService;

    public BudgetNotificationService(
        IBudgetService budgetService,
        ICategoryBudgetService categoryBudgetService,
        IExpenseQueryService expenseQueryService,
        INotificationService notificationService)
    {
        _budgetService = budgetService ?? throw new ArgumentNullException(nameof(budgetService));
        _categoryBudgetService = categoryBudgetService ?? throw new ArgumentNullException(nameof(categoryBudgetService));
        _expenseQueryService = expenseQueryService ?? throw new ArgumentNullException(nameof(expenseQueryService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    }

    public async Task EvaluateAndCreateNotificationsAsync(string userId, int month, int year)
    {
        if (string.IsNullOrWhiteSpace(userId)) return;

        var thisMonthTotals = await _expenseQueryService.GetMonthTotalsByCurrencyAsync(userId, month, year);
        var categorySpendForMonth = await _categoryBudgetService.GetCategorySpendForMonthAsync(userId, month, year);
        var categoryBudgets = await _categoryBudgetService.GetForMonthAsync(userId, month, year);

        var currentMonthBudget = await _budgetService.GetBudgetForMonthAsync(userId, month, year);
        var budgetAmount = currentMonthBudget?.Amount;
        var budgetCurrency = currentMonthBudget?.Currency ?? Currency.TZS;
        var thisMonthSpendInBudgetCurrency = thisMonthTotals.GetValueOrDefault(budgetCurrency, 0);
        var isOverBudget = budgetAmount.HasValue && budgetAmount.Value > 0 && thisMonthSpendInBudgetCurrency >= budgetAmount.Value;

        foreach (var cb in categoryBudgets)
        {
            var spent = categorySpendForMonth.GetValueOrDefault((cb.CategoryId, cb.Currency), 0);
            var catName = cb.Category?.Name ?? "Unknown";
            var topicKey = $"budget-category-{cb.CategoryId}-{year}-{month}";

            if (spent >= cb.Amount)
            {
                await _notificationService.CreateIfNotExistsAsync(userId,
                    "Category budget exceeded",
                    $"{catName}: {spent:N0} {cb.Currency} of {cb.Amount:N0} {cb.Currency} ({(spent / cb.Amount * 100):F0}%).",
                    NotificationType.CategoryBudgetExceeded, "/Budget", topicKey);
            }
            else if (cb.Amount > 0 && spent >= cb.Amount * 0.8m)
            {
                await _notificationService.CreateIfNotExistsAsync(userId,
                    "Category budget warning",
                    $"{catName}: {spent:N0} {cb.Currency} of {cb.Amount:N0} {cb.Currency} ({(spent / cb.Amount * 100):F0}%).",
                    NotificationType.CategoryBudgetWarning, "/Budget", topicKey);
            }
        }

        if (isOverBudget && budgetAmount.HasValue)
        {
            var topicKey = $"budget-global-{year}-{month}";
            await _notificationService.CreateIfNotExistsAsync(userId,
                "Monthly budget exceeded",
                $"You've spent {thisMonthSpendInBudgetCurrency:N0} {budgetCurrency} against a budget of {budgetAmount.Value:N0} {budgetCurrency}.",
                NotificationType.BudgetExceeded, "/Budget", topicKey);
        }
    }
}
