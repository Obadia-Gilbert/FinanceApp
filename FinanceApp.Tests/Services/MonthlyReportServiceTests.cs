using FinanceApp.Application.Common;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using Moq;
using Xunit;

namespace FinanceApp.Tests.Services;

/// <summary>
/// Regression cover for a report bug where a budget's amount was shown next to the
/// report's dominant-spend currency label instead of its own — e.g. a 100,000 TZS budget
/// rendered as "100,000 USD" whenever most of the user's spend happened to be in USD.
/// </summary>
public class MonthlyReportServiceTests
{
    private const string UserId = "user-1";
    private const int Month = 8;
    private const int Year = 2026;

    private readonly Mock<IExpenseQueryService> _expenses = new();
    private readonly Mock<ITransactionService> _transactions = new();
    private readonly Mock<IBudgetService> _budgets = new();
    private readonly Mock<ICategoryBudgetService> _categoryBudgets = new();
    private readonly Mock<ICategoryService> _categories = new();
    private readonly MonthlyReportService _sut;

    /// <summary>1 USD = 2500 TZS.</summary>
    private sealed class FakeCurrencyConversion : ICurrencyConversionService
    {
        private static decimal ToUsd(Currency c) => c switch { Currency.USD => 1m, Currency.TZS => 0.0004m, _ => 1m };

        public decimal ConvertToUsd(decimal amount, Currency currency) => amount * ToUsd(currency);

        public decimal Convert(decimal amount, Currency from, Currency to)
            => from == to ? amount : Math.Round(amount * ToUsd(from) / ToUsd(to), 2, MidpointRounding.AwayFromZero);

        public decimal SumInCurrency(IEnumerable<KeyValuePair<Currency, decimal>> totals, Currency target)
            => totals?.Sum(kv => Convert(kv.Value, kv.Key, target)) ?? 0m;

        public decimal SumCategoryInCurrency(
            IEnumerable<KeyValuePair<(Guid CategoryId, Currency Currency), decimal>> spend,
            Guid categoryId, Currency target)
            => spend?.Where(kv => kv.Key.CategoryId == categoryId)
                     .Sum(kv => Convert(kv.Value, kv.Key.Currency, target)) ?? 0m;
    }

    public MonthlyReportServiceTests()
    {
        _categoryBudgets.Setup(c => c.GetCategorySpendForMonthAsync(UserId, Month, Year))
            .ReturnsAsync(new Dictionary<(Guid, Currency), decimal>());
        _categoryBudgets.Setup(c => c.GetForMonthAsync(UserId, Month, Year))
            .ReturnsAsync(Array.Empty<CategoryBudget>());
        _expenses.Setup(e => e.GetCategoryTotalsForMonthAsync(UserId, Month, Year, null))
            .ReturnsAsync(Array.Empty<CategoryTotalDto>());
        _expenses.Setup(e => e.GetTopExpensesForMonthAsync(UserId, Month, Year, It.IsAny<int>()))
            .ReturnsAsync(Array.Empty<Expense>());
        _transactions.Setup(t => t.GetPagedAsync(UserId, 1, 500, It.IsAny<System.Linq.Expressions.Expression<Func<Transaction, bool>>>()))
            .ReturnsAsync(new PagedResult<Transaction> { Items = Array.Empty<Transaction>(), PageNumber = 1, PageSize = 500, TotalItems = 0 });

        _sut = new MonthlyReportService(
            _expenses.Object, _transactions.Object, _budgets.Object,
            _categoryBudgets.Object, _categories.Object, new FakeCurrencyConversion());
    }

    private void GivenMonthSpend(params (Currency Currency, decimal Amount)[] spend)
        => _expenses.Setup(e => e.GetMonthTotalsByCurrencyAsync(UserId, Month, Year))
            .ReturnsAsync(spend.ToDictionary(s => s.Currency, s => s.Amount));

    private void GivenGlobalBudget(decimal amount, Currency currency)
        => _budgets.Setup(b => b.GetBudgetForMonthAsync(UserId, Month, Year))
            .ReturnsAsync(new Budget(UserId, Month, Year, amount, currency));

    [Fact]
    public async Task GlobalBudget_InDifferentCurrencyThanReport_IsConvertedNotShownRaw()
    {
        // Budget: 100,000 TZS. Dominant spend: USD, so the report renders in USD.
        // The raw 100,000 must NOT appear labelled "USD" — it must be converted:
        // 100,000 TZS * 0.0004 = 40 USD.
        GivenGlobalBudget(100_000m, Currency.TZS);
        GivenMonthSpend((Currency.USD, 30m));
        _budgets.Setup(b => b.GetBudgetForMonthAsync(UserId, Month, Year))
            .ReturnsAsync(new Budget(UserId, Month, Year, 100_000m, Currency.TZS));

        var report = await _sut.GetMonthlyReportAsync(UserId, Year, Month, preferredCurrency: "USD");

        Assert.Equal("USD", report.Currency);
        Assert.Equal(40m, report.GlobalBudgetAmount);
    }

    [Fact]
    public async Task GlobalBudget_SameCurrencyAsReport_IsUnchanged()
    {
        GivenGlobalBudget(500m, Currency.USD);
        GivenMonthSpend((Currency.USD, 100m));

        var report = await _sut.GetMonthlyReportAsync(UserId, Year, Month, preferredCurrency: "USD");

        Assert.Equal(500m, report.GlobalBudgetAmount);
        Assert.Equal(100m, report.GlobalBudgetSpent);
        Assert.Equal(400m, report.GlobalBudgetRemaining);
    }

    [Fact]
    public async Task GlobalBudget_OverBudgetDecision_UsesBudgetCurrencyNotReportCurrency()
    {
        // Comparison must happen in the budget's own currency (TZS): 30 USD == 75,000 TZS,
        // which exceeds a 50,000 TZS budget — regardless of what currency the report displays.
        GivenGlobalBudget(50_000m, Currency.TZS);
        GivenMonthSpend((Currency.USD, 30m));

        var report = await _sut.GetMonthlyReportAsync(UserId, Year, Month, preferredCurrency: "USD");

        Assert.True(report.IsOverGlobalBudget);
    }

    [Fact]
    public async Task TotalSpent_AggregatesAcrossCurrencies_IntoReportCurrency()
    {
        // 10 USD + 5,000 TZS (== 2 USD) => 12 USD total, not just the 10 USD row.
        GivenMonthSpend((Currency.USD, 10m), (Currency.TZS, 5_000m));

        var report = await _sut.GetMonthlyReportAsync(UserId, Year, Month, preferredCurrency: "USD");

        Assert.Equal(12m, report.TotalSpent);
    }

    [Fact]
    public async Task NoBudgetSet_GlobalBudgetFieldsAreNull()
    {
        GivenMonthSpend((Currency.USD, 10m));
        _budgets.Setup(b => b.GetBudgetForMonthAsync(UserId, Month, Year)).ReturnsAsync((Budget?)null);

        var report = await _sut.GetMonthlyReportAsync(UserId, Year, Month, preferredCurrency: "USD");

        Assert.Null(report.GlobalBudgetAmount);
        Assert.Null(report.GlobalBudgetSpent);
        Assert.Null(report.GlobalBudgetRemaining);
        Assert.False(report.IsOverGlobalBudget);
    }
}
