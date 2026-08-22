using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace FinanceApp.Infrastructure.Services;

/// <summary>
/// Thread-safe, in-memory home for exchange rates. Resolves each currency through three
/// tiers, in order: a live rate fetched by <see cref="ExchangeRateApiProvider"/> /
/// <c>ExchangeRateRefreshJob</c>, a manually configured override
/// ("ExchangeRates:{CODE}" in appsettings/user-secrets/env), and finally a hardcoded
/// default — so conversions keep working even before the first successful fetch, or if
/// the live source is ever unreachable.
/// </summary>
public class ExchangeRateStore : IExchangeRateStore
{
    private readonly IConfiguration _configuration;

    // Reference swapped atomically by SetLiveRates; reads never see a half-updated dict.
    private volatile IReadOnlyDictionary<Currency, decimal>? _liveRatesToUsd;

    public ExchangeRateStore(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public DateTimeOffset? LastLiveUpdateUtc { get; private set; }

    public void SetLiveRates(IReadOnlyDictionary<Currency, decimal> ratesToUsd, DateTimeOffset fetchedAtUtc)
    {
        _liveRatesToUsd = ratesToUsd;
        LastLiveUpdateUtc = fetchedAtUtc;
    }

    public decimal GetRateToUsd(Currency currency)
    {
        var live = _liveRatesToUsd;
        if (live is not null && live.TryGetValue(currency, out var liveRate))
            return liveRate;

        var configured = _configuration.GetSection("ExchangeRates").GetValue<decimal?>(currency.ToString());
        if (configured.HasValue)
            return configured.Value;

        return GetDefaultRateToUsd(currency);
    }

    /// <summary>
    /// Fallback rates used before the first live fetch succeeds, or when both the live
    /// source and a manual override are unavailable for a given currency. 1 unit of
    /// currency = X USD. Update as needed.
    /// </summary>
    private static decimal GetDefaultRateToUsd(Currency currency)
    {
        return currency switch
        {
            Currency.USD => 1m,
            Currency.EUR => 1.08m,
            Currency.GBP => 1.27m,
            Currency.JPY => 0.0067m,
            Currency.AUD => 0.65m,
            Currency.CAD => 0.74m,
            Currency.CHF => 1.12m,
            Currency.TZS => 0.00038m,   // ~1 USD = 2630 TZS
            Currency.UGX => 0.00027m,
            Currency.KES => 0.0077m,
            Currency.RWF => 0.00078m,
            Currency.ZAR => 0.055m,
            Currency.CNY => 0.14m,
            Currency.INR => 0.012m,
            Currency.BRL => 0.20m,
            Currency.MXN => 0.058m,
            _ => 1m
        };
    }
}
