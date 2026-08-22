using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using Moq;
using Xunit;

namespace FinanceApp.Tests.Services;

/// <summary>
/// Regression cover for budget alerts that never fired when spend was recorded in a
/// different currency than the budget.
/// </summary>
public class BudgetNotificationServiceTests
{
    private const string UserId = "user-1";
    private const int Month = 8;
    private const int Year = 2026;

    private readonly Mock<IBudgetService> _budgets = new();
    private readonly Mock<ICategoryBudgetService> _categoryBudgets = new();
    private readonly Mock<IExpenseQueryService> _expenses = new();
    private readonly Mock<INotificationService> _notifications = new();
    private readonly BudgetNotificationService _sut;

    /// <summary>Fixed-rate stub: 1 USD = 2500 TZS. Keeps the test independent of real rates.</summary>
    private sealed class FakeCurrencyConversion : ICurrencyConversionService
    {
        private static decimal ToUsd(Currency c) => c switch
        {
            Currency.USD => 1m,
            Currency.TZS => 0.0004m,
            _ => 1m
        };

        public decimal ConvertToUsd(decimal amount, Currency currency) => amount * ToUsd(currency);

        public decimal Convert(decimal amount, Currency from, Currency to)
            => from == to ? amount : Math.Round(amount * ToUsd(from) / ToUsd(to), 2, MidpointRounding.AwayFromZero);

        public decimal SumInCurrency(IEnumerable<KeyValuePair<Currency, decimal>> totals, Currency target)
            => totals?.Sum(kv => Convert(kv.Value, kv.Key, target)) ?? 0m;

        public decimal SumCategoryInCurrency(
            IEnumerable<KeyValuePair<(Guid CategoryId, Currency Currency), decimal>> spend,
            Guid categoryId,
            Currency target)
            => spend?.Where(kv => kv.Key.CategoryId == categoryId)
                     .Sum(kv => Convert(kv.Value, kv.Key.Currency, target)) ?? 0m;
    }

    public BudgetNotificationServiceTests()
    {
        _notifications
            .Setup(n => n.CreateIfNotExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                                                 It.IsAny<NotificationType>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync((Notification?)null);

        _categoryBudgets.Setup(c => c.GetForMonthAsync(UserId, Month, Year))
            .ReturnsAsync(Array.Empty<CategoryBudget>());
        _categoryBudgets.Setup(c => c.GetCategorySpendForMonthAsync(UserId, Month, Year))
            .ReturnsAsync(new Dictionary<(Guid, Currency), decimal>());

        _sut = new BudgetNotificationService(
            _budgets.Object, _categoryBudgets.Object, _expenses.Object,
            _notifications.Object, new FakeCurrencyConversion());
    }

    private void GivenBudget(decimal amount, Currency currency)
        => _budgets.Setup(b => b.GetBudgetForMonthAsync(UserId, Month, Year))
                   .ReturnsAsync(new Budget(UserId, Month, Year, amount, currency));

    private void GivenMonthSpend(params (Currency Currency, decimal Amount)[] spend)
        => _expenses.Setup(e => e.GetMonthTotalsByCurrencyAsync(UserId, Month, Year))
                    .ReturnsAsync(spend.ToDictionary(s => s.Currency, s => s.Amount));

    private void AssertBudgetExceededRaised(Times times)
        => _notifications.Verify(n => n.CreateIfNotExistsAsync(
            UserId, It.IsAny<string>(), It.IsAny<string>(),
            NotificationType.BudgetExceeded, It.IsAny<string?>(), It.IsAny<string?>()), times);

    [Fact]
    public async Task GlobalBudget_SpendInDifferentCurrency_StillRaisesExceededNotification()
    {
        // Budget 100,000 TZS; spend 50 USD == 125,000 TZS -> over budget.
        // Before the fix this looked up only the TZS bucket, found nothing, and stayed silent.
        GivenBudget(100_000m, Currency.TZS);
        GivenMonthSpend((Currency.USD, 50m));

        await _sut.EvaluateAndCreateNotificationsAsync(UserId, Month, Year);

        AssertBudgetExceededRaised(Times.Once());
    }

    [Fact]
    public async Task GlobalBudget_MixedCurrencySpend_CombinesBothBeforeComparing()
    {
        // 20 USD (50,000 TZS) + 60,000 TZS = 110,000 TZS against a 100,000 TZS budget.
        // Neither bucket alone exceeds it; only the combined total does.
        GivenBudget(100_000m, Currency.TZS);
        GivenMonthSpend((Currency.USD, 20m), (Currency.TZS, 60_000m));

        await _sut.EvaluateAndCreateNotificationsAsync(UserId, Month, Year);

        AssertBudgetExceededRaised(Times.Once());
    }

    [Fact]
    public async Task GlobalBudget_SpendBelowLimit_RaisesNothing()
    {
        // 10 USD == 25,000 TZS, well under the 100,000 TZS budget.
        GivenBudget(100_000m, Currency.TZS);
        GivenMonthSpend((Currency.USD, 10m));

        await _sut.EvaluateAndCreateNotificationsAsync(UserId, Month, Year);

        AssertBudgetExceededRaised(Times.Never());
    }

    [Fact]
    public async Task NoBudgetSet_RaisesNothing()
    {
        _budgets.Setup(b => b.GetBudgetForMonthAsync(UserId, Month, Year)).ReturnsAsync((Budget?)null);
        GivenMonthSpend((Currency.USD, 5_000m));

        await _sut.EvaluateAndCreateNotificationsAsync(UserId, Month, Year);

        AssertBudgetExceededRaised(Times.Never());
    }

    [Fact]
    public async Task CategoryBudget_SpendInDifferentCurrency_StillRaisesExceededNotification()
    {
        var categoryId = Guid.NewGuid();
        GivenBudget(10_000_000m, Currency.TZS); // deliberately huge so only the category trips
        GivenMonthSpend((Currency.USD, 50m));

        _categoryBudgets.Setup(c => c.GetForMonthAsync(UserId, Month, Year))
            .ReturnsAsync(new[] { new CategoryBudget(UserId, categoryId, Month, Year, 100_000m, Currency.TZS) });
        _categoryBudgets.Setup(c => c.GetCategorySpendForMonthAsync(UserId, Month, Year))
            .ReturnsAsync(new Dictionary<(Guid, Currency), decimal> { [(categoryId, Currency.USD)] = 50m });

        await _sut.EvaluateAndCreateNotificationsAsync(UserId, Month, Year);

        _notifications.Verify(n => n.CreateIfNotExistsAsync(
            UserId, It.IsAny<string>(), It.IsAny<string>(),
            NotificationType.CategoryBudgetExceeded, It.IsAny<string?>(), It.IsAny<string?>()), Times.Once());
    }

    [Fact]
    public async Task EmptyUserId_IsIgnored()
    {
        await _sut.EvaluateAndCreateNotificationsAsync("", Month, Year);

        _notifications.VerifyNoOtherCalls();
    }
}
