using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using Xunit;

namespace FinanceApp.Tests.Services;

public class BudgetTests
{
    private static Budget NewBudget(decimal amount = 100m, Currency currency = Currency.USD)
        => new("user-1", 8, 2026, amount, currency);

    [Fact]
    public void UpdateAmount_ChangesAmount()
    {
        var budget = NewBudget(100m);

        budget.UpdateAmount(250m, Currency.USD);

        Assert.Equal(250m, budget.Amount);
    }

    [Fact]
    public void UpdateAmount_ChangesCurrency()
    {
        // Regression: the update path previously ignored the currency argument entirely,
        // so a budget could never be switched to another currency once created.
        var budget = NewBudget(100m, Currency.USD);

        budget.UpdateAmount(100_000m, Currency.TZS);

        Assert.Equal(Currency.TZS, budget.Currency);
        Assert.Equal(100_000m, budget.Amount);
    }

    [Fact]
    public void UpdateAmount_NegativeAmount_Throws()
    {
        var budget = NewBudget();

        Assert.Throws<ArgumentException>(() => budget.UpdateAmount(-1m, Currency.USD));
    }

    [Fact]
    public void UpdateAmount_ZeroIsAllowed()
    {
        var budget = NewBudget();

        budget.UpdateAmount(0m, Currency.EUR);

        Assert.Equal(0m, budget.Amount);
        Assert.Equal(Currency.EUR, budget.Currency);
    }
}
