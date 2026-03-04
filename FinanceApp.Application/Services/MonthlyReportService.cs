using System.Globalization;
using FinanceApp.Application.Common;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Services;

public class MonthlyReportService : IMonthlyReportService
{
    private readonly IExpenseQueryService _expenseQueryService;
    private readonly ITransactionService _transactionService;
    private readonly IBudgetService _budgetService;
    private readonly ICategoryBudgetService _categoryBudgetService;
    private readonly ICategoryService _categoryService;

    public MonthlyReportService(
        IExpenseQueryService expenseQueryService,
        ITransactionService transactionService,
        IBudgetService budgetService,
        ICategoryBudgetService categoryBudgetService,
        ICategoryService categoryService)
    {
        _expenseQueryService = expenseQueryService;
        _transactionService = transactionService;
        _budgetService = budgetService;
        _categoryBudgetService = categoryBudgetService;
        _categoryService = categoryService;
    }

    public async Task<MonthlyReportResult> GetMonthlyReportAsync(string userId, int year, int month, string? preferredCurrency = null, int topExpensesCount = 20)
    {
        var monthName = new DateTime(year, month, 1).ToString("MMMM yyyy", CultureInfo.InvariantCulture);

        // Sequential: all use the same scoped DbContext
        var monthTotals = await _expenseQueryService.GetMonthTotalsByCurrencyAsync(userId, month, year);
        var categorySpendForMonth = await _categoryBudgetService.GetCategorySpendForMonthAsync(userId, month, year);
        var categoryTotals = await _expenseQueryService.GetCategoryTotalsForMonthAsync(userId, month, year, null);
        var topExpensesList = await _expenseQueryService.GetTopExpensesForMonthAsync(userId, month, year, topExpensesCount);
        var categoryBudgets = (await _categoryBudgetService.GetForMonthAsync(userId, month, year)).ToList();
        var globalBudget = await _budgetService.GetBudgetForMonthAsync(userId, month, year);
        var incomeTx = await _transactionService.GetPagedAsync(userId, 1, 500,
            t => t.Date.Month == month && t.Date.Year == year && t.Type == TransactionType.Income);

        var currency = preferredCurrency;
        if (string.IsNullOrEmpty(currency))
            currency = monthTotals.Count > 0
                ? monthTotals.OrderByDescending(kv => kv.Value).Select(kv => kv.Key.ToString()).First()
                : Currency.TZS.ToString();

        var currencyEnum = Enum.TryParse<Currency>(currency, true, out var c) ? c : Currency.TZS;
        var totalSpent = monthTotals.GetValueOrDefault(currencyEnum, 0);

        var totalIncome = incomeTx.Items.Where(t => t.Currency == currencyEnum).Sum(t => t.Amount);

        decimal? globalBudgetAmount = globalBudget?.Amount;
        var budgetCurrency = globalBudget?.Currency ?? currencyEnum;
        var globalSpent = globalBudgetAmount.HasValue ? monthTotals.GetValueOrDefault(budgetCurrency, 0) : (decimal?)null;
        decimal? globalRemaining = globalBudgetAmount.HasValue && globalSpent.HasValue ? globalBudgetAmount.Value - globalSpent.Value : null;
        var isOverGlobal = globalBudgetAmount.HasValue && globalSpent.HasValue && globalSpent.Value >= globalBudgetAmount.Value;

        var categoryLines = new List<CategoryReportLine>();
        foreach (var cb in categoryBudgets)
        {
            var spent = categorySpendForMonth.GetValueOrDefault((cb.CategoryId, cb.Currency), 0);
            categoryLines.Add(new CategoryReportLine
            {
                CategoryName = cb.Category?.Name ?? "Unknown",
                Spent = spent,
                BudgetAmount = cb.Amount,
                Remaining = cb.Amount - spent,
                IsOverBudget = spent >= cb.Amount
            });
        }

        var budgetCategoryIds = categoryBudgets.Select(cb => cb.CategoryId).ToHashSet();
        foreach (var ct in categoryTotals.Where(ct => !budgetCategoryIds.Contains(ct.CategoryId)))
        {
            categoryLines.Add(new CategoryReportLine
            {
                CategoryName = ct.CategoryName ?? "Unknown",
                Spent = ct.Sum,
                BudgetAmount = null,
                Remaining = null,
                IsOverBudget = false
            });
        }

        categoryLines = categoryLines.OrderByDescending(cl => cl.Spent).ToList();

        var topExpenses = topExpensesList.Select(e => new ExpenseReportLine
        {
            Description = e.Description ?? "",
            Amount = e.Amount,
            Currency = e.Currency.ToString(),
            Date = e.ExpenseDate.DateTime,
            CategoryName = e.Category?.Name ?? "Unknown"
        }).ToList();

        return new MonthlyReportResult
        {
            Month = month,
            Year = year,
            MonthName = monthName,
            TotalSpent = totalSpent,
            TotalIncome = totalIncome,
            Currency = currency,
            GlobalBudgetAmount = globalBudgetAmount,
            GlobalBudgetSpent = globalSpent,
            GlobalBudgetRemaining = globalRemaining,
            IsOverGlobalBudget = isOverGlobal,
            CategoryLines = categoryLines,
            TopExpenses = topExpenses
        };
    }
}
