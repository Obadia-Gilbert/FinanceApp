namespace FinanceApp.Infrastructure.Services;

/// <summary>
/// Configuration for periodic live exchange-rate fetching via <see cref="ExchangeRateApiProvider"/>.
/// Bind from the "ExchangeRates:Provider" section (<c>Shared/appsettings.shared.json</c>).
/// </summary>
public class ExchangeRateSettings
{
    /// <summary>
    /// Free, no-key "Open Access" endpoint — https://www.exchangerate-api.com/docs/free.
    /// Returns 1 USD = X target for ~160 ISO-4217 codes, updated daily.
    /// </summary>
    public string BaseUrl { get; set; } = "https://open.er-api.com/v6/latest/USD";

    /// <summary>How often the background job re-fetches. The source itself updates once a day.</summary>
    public int RefreshIntervalHours { get; set; } = 8;

    /// <summary>HTTP timeout in milliseconds.</summary>
    public int TimeoutMs { get; set; } = 10000;
}
