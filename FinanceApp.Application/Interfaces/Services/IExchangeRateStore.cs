using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Interfaces.Services;

/// <summary>
/// Resolves the exchange rate for a currency (1 unit of currency = X USD), preferring
/// the most recently fetched live rate, then a manually configured override, then a
/// hardcoded default — so a conversion never fails just because live data hasn't
/// arrived yet or a fetch failed.
/// </summary>
public interface IExchangeRateStore
{
    /// <summary>1 unit of <paramref name="currency"/> = X USD.</summary>
    decimal GetRateToUsd(Currency currency);

    /// <summary>
    /// Replaces the live tier with a freshly fetched set of rates. Called by
    /// <c>ExchangeRateRefreshJob</c> after a successful fetch; currencies absent from
    /// <paramref name="ratesToUsd"/> keep resolving through the configured/default tiers.
    /// </summary>
    void SetLiveRates(IReadOnlyDictionary<Currency, decimal> ratesToUsd, DateTimeOffset fetchedAtUtc);

    /// <summary>When the live tier was last successfully refreshed, or null before the first success.</summary>
    DateTimeOffset? LastLiveUpdateUtc { get; }
}
