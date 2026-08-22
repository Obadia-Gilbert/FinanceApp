using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Interfaces.Services;

/// <summary>
/// Fetches live exchange rates from an external source.
/// </summary>
public interface IExchangeRateProvider
{
    /// <summary>
    /// Returns 1-unit-of-currency-= X-USD rates for every <see cref="Currency"/> the
    /// source reports, or <c>null</c> on any failure (network, non-success response,
    /// parse error) — this method never throws.
    /// </summary>
    Task<IReadOnlyDictionary<Currency, decimal>?> FetchRatesToUsdAsync(CancellationToken cancellationToken);
}
