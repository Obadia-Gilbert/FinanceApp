using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FinanceApp.API.Tests.Unit;

/// <summary>
/// Covers the three-tier rate resolution order: live-fetched > configured override >
/// hardcoded default. This is the fallback chain that lets conversions keep working
/// before the first successful live fetch, or if the live source is ever unreachable.
/// </summary>
public class ExchangeRateStoreTests
{
    private static ExchangeRateStore BuildWithConfig(params (string Code, string Rate)[] rates)
    {
        var values = rates.ToDictionary(r => $"ExchangeRates:{r.Code}", r => (string?)r.Rate);
        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        return new ExchangeRateStore(config);
    }

    [Fact]
    public void GetRateToUsd_NothingSetAnywhere_FallsBackToHardcodedDefault()
    {
        var sut = BuildWithConfig();

        var result = sut.GetRateToUsd(Currency.EUR);

        Assert.True(result > 0m, "EUR should resolve via the built-in default table, not zero.");
        Assert.Null(sut.LastLiveUpdateUtc);
    }

    [Fact]
    public void GetRateToUsd_ConfiguredOverride_UsedWhenNoLiveRateSet()
    {
        var sut = BuildWithConfig(("TZS", "0.00041"));

        Assert.Equal(0.00041m, sut.GetRateToUsd(Currency.TZS));
    }

    [Fact]
    public void GetRateToUsd_LiveRate_TakesPriorityOverConfiguredOverride()
    {
        var sut = BuildWithConfig(("TZS", "0.00041"));
        var fetchedAt = DateTimeOffset.UtcNow;

        sut.SetLiveRates(new Dictionary<Currency, decimal> { [Currency.TZS] = 0.00039m }, fetchedAt);

        Assert.Equal(0.00039m, sut.GetRateToUsd(Currency.TZS));
        Assert.Equal(fetchedAt, sut.LastLiveUpdateUtc);
    }

    [Fact]
    public void GetRateToUsd_LiveRateMissingForCurrency_FallsBackToConfiguredOrDefault()
    {
        var sut = BuildWithConfig(("TZS", "0.00041"));

        // Live fetch only reported USD and EUR — TZS must still resolve via tier 2/3.
        sut.SetLiveRates(new Dictionary<Currency, decimal>
        {
            [Currency.USD] = 1m,
            [Currency.EUR] = 1.09m
        }, DateTimeOffset.UtcNow);

        Assert.Equal(0.00041m, sut.GetRateToUsd(Currency.TZS));
    }
}
