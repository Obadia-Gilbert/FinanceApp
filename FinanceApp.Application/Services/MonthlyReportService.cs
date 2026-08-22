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
    private readonly ICurrencyConversionService _currencyConversion;

    public MonthlyReportService(
        IExpenseQueryService expenseQueryService,
        ITransactionService transactionService,
        IBudgetService budgetService,
        ICategoryBudgetService categoryBudgetService,
        ICategoryService categoryService,
        ICurrencyConversionService currencyConversion)
    {
        _expenseQueryService = expenseQueryService;
        _transactionService = transactionService;
        _budgetService = budgetService;
        _categoryBudgetService = categoryBudgetService;
        _categoryService = categoryService;
        _currencyConversion = currencyConversion;
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

        // Spend and income can be recorded in several currencies — convert each into the
        // report currency rather than counting only the rows that already match it.
        var totalSpent = _currencyConversion.SumInCurrency(monthTotals, currencyEnum);

        var totalIncome = incomeTx.Items.Sum(t => _currencyConversion.Convert(t.Amount, t.Currency, currencyEnum));

        // Compare budget vs. spend in the budget's own currency (no conversion rounding
        // in the pass/fail decision), then convert both into the report's display currency
        // so every figure on the page carries the same, correctly-labelled unit — a budget
        // set in TZS must not have its number shown next to a "USD" label just because most
        // of the user's spend happens to be in USD.
        decimal? globalBudgetAmount = globalBudget?.Amount;
        var budgetCurrency = globalBudget?.Currency ?? currencyEnum;
        var globalSpentInBudgetCurrency = globalBudgetAmount.HasValue
            ? _currencyConversion.SumInCurrency(monthTotals, budgetCurrency)
            : (decimal?)null;
        var isOverGlobal = globalBudgetAmount.HasValue && globalSpentInBudgetCurrency.HasValue
            && globalSpentInBudgetCurrency.Value >= globalBudgetAmount.Value;

        decimal? globalBudgetAmountDisplay = globalBudgetAmount.HasValue
            ? _currencyConversion.Convert(globalBudgetAmount.Value, budgetCurrency, currencyEnum)
            : null;
        decimal? globalSpentDisplay = globalSpentInBudgetCurrency.HasValue
            ? _currencyConversion.Convert(globalSpentInBudgetCurrency.Value, budgetCurrency, currencyEnum)
            : null;
        decimal? globalRemaining = globalBudgetAmountDisplay.HasValue && globalSpentDisplay.HasValue
            ? globalBudgetAmountDisplay.Value - globalSpentDisplay.Value
            : null;

        var categoryLines = new List<CategoryReportLine>();
        foreach (var cb in categoryBudgets)
        {
            var spent = _currencyConversion.SumCategoryInCurrency(categorySpendForMonth, cb.CategoryId, cb.Currency);
            var isOver = spent >= cb.Amount;

            var spentDisplay = _currencyConversion.Convert(spent, cb.Currency, currencyEnum);
            var budgetDisplay = _currencyConversion.Convert(cb.Amount, cb.Currency, currencyEnum);
            categoryLines.Add(new CategoryReportLine
            {
                CategoryName = cb.Category?.Name ?? "Unknown",
                Spent = spentDisplay,
                BudgetAmount = budgetDisplay,
                Remaining = budgetDisplay - spentDisplay,
                IsOverBudget = isOver
            });
        }

        // categoryTotals has one row per (category, currency); collapse each category into
        // a single line converted to the report currency.
        var budgetCategoryIds = categoryBudgets.Select(cb => cb.CategoryId).ToHashSet();
        foreach (var group in categoryTotals
            .Where(ct => !budgetCategoryIds.Contains(ct.CategoryId))
            .GroupBy(ct => ct.CategoryId))
        {
            categoryLines.Add(new CategoryReportLine
            {
                CategoryName = group.First().CategoryName ?? "Unknown",
                Spent = group.Sum(ct => _currencyConversion.Convert(ct.Sum, ct.Currency, currencyEnum)),
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
            GlobalBudgetAmount = globalBudgetAmountDisplay,
            GlobalBudgetSpent = globalSpentDisplay,
            GlobalBudgetRemaining = globalRemaining,
            IsOverGlobalBudget = isOverGlobal,
            CategoryLines = categoryLines,
            TopExpenses = topExpenses
        };
    }
}
