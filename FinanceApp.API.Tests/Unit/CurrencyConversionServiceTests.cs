using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FinanceApp.API.Tests.Unit;

/// <summary>
/// Covers the conversion helpers that budget, dashboard and report totals rely on.
/// The <c>SumInCurrency</c> tests are regression cover for a bug where spend recorded in
/// a currency other than the budget's own was silently counted as zero.
/// </summary>
public class CurrencyConversionServiceTests
{
    /// <summary>Rates are "1 unit of currency = X USD", matching appsettings "ExchangeRates".</summary>
    private static CurrencyConversionService Build(params (string Code, string Rate)[] rates)
    {
        var values = rates.ToDictionary(r => $"ExchangeRates:{r.Code}", r => (string?)r.Rate);
        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var store = new ExchangeRateStore(config);
        return new CurrencyConversionService(store);
    }

    // 1 USD = 2500 TZS  ->  1 TZS = 0.0004 USD
    private static CurrencyConversionService BuildUsdTzs()
        => Build(("USD", "1"), ("TZS", "0.0004"));

    [Fact]
    public void Convert_SameCurrency_ReturnsAmountUnchanged()
    {
        var sut = BuildUsdTzs();

        Assert.Equal(42.50m, sut.Convert(42.50m, Currency.USD, Currency.USD));
    }

    [Fact]
    public void Convert_UsdToTzs_UsesConfiguredRate()
    {
        var sut = BuildUsdTzs();

        // 10 USD -> 10 / 0.0004 = 25,000 TZS
        Assert.Equal(25000m, sut.Convert(10m, Currency.USD, Currency.TZS));
    }

    [Fact]
    public void Convert_TzsToUsd_UsesConfiguredRate()
    {
        var sut = BuildUsdTzs();

        // 25,000 TZS -> 25,000 * 0.0004 = 10 USD
        Assert.Equal(10m, sut.Convert(25000m, Currency.TZS, Currency.USD));
    }

    [Fact]
    public void Convert_ZeroAmount_ReturnsZero()
    {
        var sut = BuildUsdTzs();

        Assert.Equal(0m, sut.Convert(0m, Currency.USD, Currency.TZS));
    }

    [Fact]
    public void SumInCurrency_ConvertsEveryCurrencyBucket_NotJustTheMatchingOne()
    {
        var sut = BuildUsdTzs();
        var totals = new Dictionary<Currency, decimal>
        {
            [Currency.USD] = 10m,      // -> 25,000 TZS
            [Currency.TZS] = 5000m     // -> 5,000 TZS
        };

        // Regression: a plain GetValueOrDefault(TZS) would have returned only 5,000
        // and silently dropped the USD spend entirely.
        Assert.Equal(30000m, sut.SumInCurrency(totals, Currency.TZS));
    }

    [Fact]
    public void SumInCurrency_TargetCurrencyAbsentFromTotals_StillCountsOtherCurrencies()
    {
        var sut = BuildUsdTzs();
        var totals = new Dictionary<Currency, decimal> { [Currency.USD] = 10m };

        // The exact bug that hid over-budget states: no TZS row at all, yet real spend exists.
        Assert.Equal(25000m, sut.SumInCurrency(totals, Currency.TZS));
    }

    [Fact]
    public void SumInCurrency_EmptyOrNull_ReturnsZero()
    {
        var sut = BuildUsdTzs();

        Assert.Equal(0m, sut.SumInCurrency(new Dictionary<Currency, decimal>(), Currency.TZS));
        Assert.Equal(0m, sut.SumInCurrency(null!, Currency.TZS));
    }

    [Fact]
    public void SumCategoryInCurrency_SumsOnlyTheRequestedCategory_AcrossCurrencies()
    {
        var sut = BuildUsdTzs();
        var groceries = Guid.NewGuid();
        var transport = Guid.NewGuid();

        var spend = new Dictionary<(Guid CategoryId, Currency Currency), decimal>
        {
            [(groceries, Currency.USD)] = 10m,     // -> 25,000 TZS
            [(groceries, Currency.TZS)] = 1000m,   // ->  1,000 TZS
            [(transport, Currency.USD)] = 99m      // must be excluded
        };

        Assert.Equal(26000m, sut.SumCategoryInCurrency(spend, groceries, Currency.TZS));
    }

    [Fact]
    public void SumCategoryInCurrency_UnknownCategory_ReturnsZero()
    {
        var sut = BuildUsdTzs();
        var spend = new Dictionary<(Guid CategoryId, Currency Currency), decimal>
        {
            [(Guid.NewGuid(), Currency.USD)] = 10m
        };

        Assert.Equal(0m, sut.SumCategoryInCurrency(spend, Guid.NewGuid(), Currency.TZS));
    }

    [Fact]
    public void UnconfiguredCurrency_FallsBackToBuiltInRate_RatherThanThrowing()
    {
        // Only USD configured; EUR must still resolve via the built-in default table.
        var sut = Build(("USD", "1"));

        var result = sut.Convert(100m, Currency.EUR, Currency.USD);

        Assert.True(result > 0m, "EUR should fall back to a built-in rate, not resolve to zero.");
    }
}
